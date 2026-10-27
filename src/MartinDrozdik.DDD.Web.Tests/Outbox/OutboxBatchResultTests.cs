using MartinDrozdik.DDD.Web.Outbox;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies when <see cref="OutboxBatchResult.MayHaveMore"/> asks for another batch.
/// </summary>
public class OutboxBatchResultTests
{
    [Theory]
    [InlineData(0, 0, 0, 0, false)] // Found nothing
    [InlineData(3, 3, 0, 3, true)] // Delivered what it found
    [InlineData(3, 3, 0, 0, true)] // Every message failed and waits for its retry
    [InlineData(3, 0, 0, 0, true)] // Another processor reserved them first
    [InlineData(3, 3, 2, 1, true)] // A slow message let the rest of the batch go
    [InlineData(3, 3, 3, 0, false)] // Handed back everything it found, the next batch would find the same
    [InlineData(3, 1, 1, 0, true)] // Handed back its own, another processor took the rest
    public void MayHaveMore_holds_while_a_batch_found_messages_and_moved_one_on(int found, int reserved, int released, int dispatched, bool expected)
    {
        // Arrange
        var result = new OutboxBatchResult(found, reserved, released, dispatched);

        // Act
        var mayHaveMore = result.MayHaveMore;

        // Assert
        Assert.Equal(expected, mayHaveMore);
    }
}
