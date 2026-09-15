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
    : SaveChangesInterceptor
{
    /// <summary>
    /// Whether the save currently running is writing new messages.
    /// </summary>
    private bool _isWritingMessages;

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
        TriggerIfNeeded();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        TriggerIfNeeded();
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

    /// <summary>
    /// Decides whether the save is writing new messages.
    /// </summary>
    /// <param name="context">The context being saved.</param>
    /// <returns>True when the context has messages waiting to be inserted, else false.</returns>
    private static bool HasNewOutboxMessages(DbContext? context)
        => context?.ChangeTracker
            .Entries<OutboxMessage>()
            .Any(entry => entry.State == EntityState.Added) == true;

    /// <summary>
    /// Wakes the dispatch task when the completed save wrote new messages.
    /// </summary>
    private void TriggerIfNeeded()
    {
        if (!_isWritingMessages)
        {
            return;
        }

        _isWritingMessages = false;
        trigger.Trigger();
    }
}
