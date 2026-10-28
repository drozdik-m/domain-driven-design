using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Testing.Outbox;

/// <summary>
/// Extensions for testing the outbox without waiting for its schedule.
/// </summary>
public static class OutboxTestExtensions
{
    /// <summary>
    /// Delivers one batch of pending messages of the outbox of <typeparamref name="TDbContext"/>, in a fresh dependency injection scope.
    /// </summary>
    /// <remarks>
    /// This is a direct invocation of <see cref="IOutboxProcessor{TDbContext}"/>, not the dispatch task, which keeps going while <see cref="OutboxBatchResult.MayHaveMore"/>.
    /// Handler failures are still swallowed into retries and dead-letters.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <param name="services">The service provider of the application.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The outcome of the batch.</returns>
    public static async Task<OutboxBatchResult> ProcessOutboxAsync<TDbContext>(this IServiceProvider services, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor<TDbContext>>();
        return await processor.ProcessPendingAsync(cancellationToken);
    }

    /// <inheritdoc cref="ProcessOutboxAsync{TDbContext}(IServiceProvider, CancellationToken)"/>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    public static Task<OutboxBatchResult> ProcessOutboxAsync<TDbContext>(this ITestedApp testedApp, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(testedApp);
        return testedApp.Services.ProcessOutboxAsync<TDbContext>(cancellationToken);
    }

    /// <summary>
    /// Deletes the delivered messages of the outbox of <typeparamref name="TDbContext"/> that are older than its retention, in a fresh dependency injection scope.
    /// </summary>
    /// <remarks>
    /// This is a direct invocation of <see cref="IOutboxRetention{TDbContext}.ApplyAsync"/>, not the retention task.
    /// The retention must be added with <see cref="HostApplicationBuilderExtensions.AddOutboxRetention{TDbContext}"/>.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <param name="services">The service provider of the application.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The number of messages deleted.</returns>
    public static async Task<int> ApplyOutboxRetentionAsync<TDbContext>(this IServiceProvider services, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var retention = scope.ServiceProvider.GetRequiredService<IOutboxRetention<TDbContext>>();
        return await retention.ApplyAsync(cancellationToken);
    }

    /// <inheritdoc cref="ApplyOutboxRetentionAsync{TDbContext}(IServiceProvider, CancellationToken)"/>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    public static Task<int> ApplyOutboxRetentionAsync<TDbContext>(this ITestedApp testedApp, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(testedApp);
        return testedApp.Services.ApplyOutboxRetentionAsync<TDbContext>(cancellationToken);
    }

    /// <summary>
    /// Delivers pending messages of the outbox of <typeparamref name="TDbContext"/> repeatedly until nothing is left to deliver.
    /// </summary>
    /// <remarks>
    /// Useful when a handler enqueues further messages, or when more messages are pending than one batch holds.
    /// Keeps going while <see cref="OutboxBatchResult.MayHaveMore"/>, as the dispatch task does.
    /// Messages waiting on a retry delay are not delivered, so this terminates.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The total number of messages delivered successfully.</returns>
    public static async Task<int> DrainOutboxAsync<TDbContext>(this ITestedApp testedApp, CancellationToken cancellationToken)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(testedApp);

        var total = 0;
        OutboxBatchResult result;
        do
        {
            result = await testedApp.Services.ProcessOutboxAsync<TDbContext>(cancellationToken);
            total += result.Dispatched;
        }
        while (result.MayHaveMore);

        return total;
    }
}
