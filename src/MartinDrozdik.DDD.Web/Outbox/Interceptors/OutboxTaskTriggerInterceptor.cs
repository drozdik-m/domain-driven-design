using System.Data.Common;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MartinDrozdik.DDD.Web.Outbox.Interceptors;

/// <summary>
/// Wakes the outbox dispatch recurring task as soon as a transaction that enqueued messages has committed.
/// </summary>
/// <remarks>
/// <para>
/// Without it a message waits up to one polling period before anything looks at it.
/// The trigger only shortens that wait, to this interceptor is not required for correctness, only for responsiveness.
/// </para>
/// <para>
/// A plain <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> commits on its own, so the trigger fires right after it.
/// Inside an explicit transaction started with <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.BeginTransactionAsync(CancellationToken)"/>
/// the save is not the commit: the trigger waits for the transaction to commit, and is dropped when it rolls back.
/// Otherwise the woken dispatch would read before the messages are visible and the trigger would be spent for nothing.
/// </para>
/// <para>
/// A commit this interceptor cannot see, such as a <see cref="DbTransaction"/> passed in with
/// <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.UseTransaction(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade, DbTransaction?)"/>
/// and committed directly, leaves the messages to the next scheduled poll.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.AddAppDbContext&lt;InvoiceDbContext&gt;((options, provider, dbBuilder) =&gt;
/// {
///     dbBuilder.UseSqlite(options.ConnectionString);
///     dbBuilder.AddInterceptors(provider.GetRequiredService&lt;OutboxTaskTriggerInterceptor&gt;());
/// });
/// </code>
/// </example>
/// <param name="trigger">The trigger of the built-in dispatch task.</param>
public sealed class OutboxTaskTriggerInterceptor(IRecurringTaskTrigger<OutboxDispatchRecurringTask> trigger)
    : SaveChangesInterceptor, IDbTransactionInterceptor
{
    /// <summary>
    /// Whether the save currently running is writing new messages.
    /// </summary>
    private bool _isWritingMessages;

    /// <summary>
    /// Whether the explicit transaction currently open has saved new messages, so its commit should wake the dispatch task.
    /// </summary>
    private bool _isTriggerPendingCommit;

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _isWritingMessages = HasNewOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _isWritingMessages = HasNewOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        TriggerIfNeeded(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        TriggerIfNeeded(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _isWritingMessages = false;
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _isWritingMessages = false;
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <inheritdoc/>
    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        _isWritingMessages = false;
        base.SaveChangesCanceled(eventData);
    }

    /// <inheritdoc/>
    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        _isWritingMessages = false;
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    /// <inheritdoc />
    public InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result)
    {
        _isTriggerPendingCommit = false;
        return result;
    }

    /// <inheritdoc />
    public ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        _isTriggerPendingCommit = false;
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        TriggerIfPendingCommit();
    }

    /// <inheritdoc />
    public Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TriggerIfPendingCommit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        _isTriggerPendingCommit = false;
    }

    /// <inheritdoc />
    public Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _isTriggerPendingCommit = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Decides whether the save is writing new messages.
    /// </summary>
    /// <param name="context">The context being saved.</param>
    /// <returns>True when the context has deliverable messages waiting to be inserted, else false.</returns>
    private static bool HasNewOutboxMessages(DbContext? context)
        => context?.ChangeTracker
            .Entries<OutboxMessage>()
            .Any(entry => entry.State == EntityState.Added && entry.Entity.AvailableAt <= entry.Entity.OccurredAt) == true; // entry.Entity.OccurredAt is basically the current time

    /// <summary>
    /// Wakes the dispatch task when the completed save wrote new messages,
    /// or leaves that to the commit when the save ran inside an explicit transaction.
    /// </summary>
    /// <param name="context">The context that was saved.</param>
    private void TriggerIfNeeded(DbContext? context)
    {
        if (!_isWritingMessages)
        {
            return;
        }

        _isWritingMessages = false;

        // The transaction SaveChanges opens on its own is already committed by now
        // This transaction is the callers
        if (context?.Database.CurrentTransaction is not null)
        {
            _isTriggerPendingCommit = true;
            return;
        }

        trigger.Trigger();
    }

    /// <summary>
    /// Wakes the dispatch task when the transaction that just committed saved new messages.
    /// </summary>
    private void TriggerIfPendingCommit()
    {
        if (!_isTriggerPendingCommit)
        {
            return;
        }

        _isTriggerPendingCommit = false;
        trigger.Trigger();
    }
}
