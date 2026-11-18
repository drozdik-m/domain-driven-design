using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies when <see cref="OutboxProcessorExtensions.DrainAsync{TDbContext}"/> asks for another batch.
/// </summary>
public class OutboxProcessorExtensionsTests
{
    private static readonly OutboxBatchResult s_nothingReserved = new(Found: 1, Reserved: 0, Released: 0, Dispatched: 0);
    private static readonly OutboxBatchResult s_delivered = new(Found: 1, Reserved: 1, Released: 0, Dispatched: 1);
    private static readonly OutboxBatchResult s_empty = new(Found: 0, Reserved: 0, Released: 0, Dispatched: 0);

    [Fact]
    public async Task DrainAsync_stops_after_three_batches_in_a_row_that_reserved_nothing()
    {
        // Arrange
        // Every batch finds a row it cannot reserve, forever
        var processor = new ScriptedProcessor(Enumerable.Repeat(s_nothingReserved, 100));

        // Act
        var dispatched = await processor.DrainAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, dispatched);
        Assert.Equal(3, processor.Calls);
    }

    [Fact]
    public async Task DrainAsync_keeps_going_when_a_batch_reserves_something_between_batches_that_reserved_nothing()
    {
        // Arrange
        var processor = new ScriptedProcessor([
            s_nothingReserved,
            s_nothingReserved,
            s_delivered,
            s_nothingReserved,
            s_nothingReserved,
            s_delivered,
            s_empty,
        ]);

        // Act
        var dispatched = await processor.DrainAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, dispatched);
        Assert.Equal(7, processor.Calls);
    }

    [Fact]
    public async Task DrainAsync_stops_once_a_batch_finds_nothing_and_returns_the_total_delivered()
    {
        // Arrange
        var processor = new ScriptedProcessor([s_delivered, s_delivered, s_empty, s_delivered]);

        // Act
        var dispatched = await processor.DrainAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, dispatched);
        Assert.Equal(3, processor.Calls);
    }

    [Fact]
    public async Task DrainAsync_stops_between_batches_once_cancelled()
    {
        // Arrange
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var processor = new ScriptedProcessor(Enumerable.Repeat(s_delivered, 100), onCall: cancellation.Cancel);

        // Act
        var dispatched = await processor.DrainAsync(cancellation.Token);

        // Assert
        Assert.Equal(1, dispatched);
        Assert.Equal(1, processor.Calls);
    }

    /// <summary>
    /// Returns the given batch results in order, one per call.
    /// </summary>
    /// <param name="results">The results to return.</param>
    /// <param name="onCall">Runs on every call, before it returns.</param>
    private sealed class ScriptedProcessor(IEnumerable<OutboxBatchResult> results, Action? onCall = null) : IOutboxProcessor<TestDbContext>
    {
        private readonly Queue<OutboxBatchResult> _results = new(results);

        public int Calls { get; private set; }

        public Task<OutboxBatchResult> ProcessPendingAsync(CancellationToken cancellationToken)
        {
            Calls++;
            onCall?.Invoke();
            return Task.FromResult(_results.Dequeue());
        }
    }
}
