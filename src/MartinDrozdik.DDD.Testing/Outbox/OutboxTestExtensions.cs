using MartinDrozdik.DDD.Web.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Testing.Outbox;

/// <summary>
/// Extensions for testing the outbox without waiting for its schedule.
/// </summary>
public static class OutboxTestExtensions
{
    /// <summary>
    /// Delivers one batch of pending outbox messages, in a fresh dependency injection scope, exactly as the dispatch task would.
    /// </summary>
    /// <remarks>
    /// This is a direct invocation of <see cref="IOutboxProcessor"/>, not the loop.
    /// Handler failures are still swallowed into retries and dead-letters.
    /// </remarks>
    /// <param name="services">The service provider of the application.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The number of messages delivered successfully.</returns>
    public static async Task<int> ProcessOutboxAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        return await processor.ProcessPendingAsync(cancellationToken);
    }

    /// <inheritdoc cref="ProcessOutboxAsync(IServiceProvider, CancellationToken)"/>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    public static Task<int> ProcessOutboxAsync(this ITestedApp testedApp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(testedApp);
        return testedApp.Services.ProcessOutboxAsync(cancellationToken);
    }

    /// <summary>
    /// Delivers pending outbox messages repeatedly until nothing is left to deliver.
    /// </summary>
    /// <remarks>
    /// Useful when a handler enqueues further messages, or when more messages are pending than one batch holds.
    /// Messages waiting on a retry delay are not delivered, so this terminates.
    /// </remarks>
    /// <param name="testedApp">The application under test.</param>
    /// <param name="cancellationToken">Cancellation token of the test.</param>
    /// <returns>The total number of messages delivered successfully.</returns>
    public static async Task<int> DrainOutboxAsync(this ITestedApp testedApp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(testedApp);

        var total = 0;
        int dispatched;
        do
        {
            dispatched = await testedApp.Services.ProcessOutboxAsync(cancellationToken);
            total += dispatched;
        }
        while (dispatched > 0);

        return total;
    }
}
