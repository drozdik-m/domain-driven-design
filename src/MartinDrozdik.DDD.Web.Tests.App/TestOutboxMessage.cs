using System.Collections.Concurrent;
using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// An arbitrary outbox message for testing.
/// </summary>
/// <param name="Text">Any payload content a test wants to assert on.</param>
public sealed record TestOutboxMessage(string Text) : IOutboxMessage
{
    /// <inheritdoc />
    public static OutboxMessageType MessageType => "test.message.v1";
}

/// <summary>
/// A second message type, so tests can prove that two types route to their own handlers.
/// </summary>
/// <param name="Number">Any payload content a test wants to assert on.</param>
public sealed record OtherTestOutboxMessage(int Number) : IOutboxMessage
{
    /// <inheritdoc />
    public static OutboxMessageType MessageType => "test.other-message.v1";
}

/// <summary>
/// Records what the outbox handlers received, and decides whether they should fail.
/// Registered as a singleton so a test can read it after the handler ran in its own scope.
/// </summary>
public sealed class TestOutboxHandlerState
{
    private readonly ConcurrentQueue<string> _handled = [];

    /// <summary>
    /// Gets the payload content of every message handled so far, in order.
    /// </summary>
    public IReadOnlyCollection<string> Handled => _handled;

    /// <summary>
    /// Gets or sets a value indicating whether the handlers should throw instead of succeeding.
    /// </summary>
    public bool ShouldFail { get; set; }

    /// <summary>
    /// Gets or sets the message of the exception thrown while <see cref="ShouldFail"/> is set.
    /// </summary>
    public string FailureMessage { get; set; } = "Delivery failed on purpose.";

    /// <summary>
    /// Records a handled message, or throws when the handlers are set to fail.
    /// </summary>
    /// <param name="content">The payload content of the handled message.</param>
    public void Handle(string content)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException(FailureMessage);
        }

        _handled.Enqueue(content);
    }
}

/// <summary>
/// Delivers <see cref="TestOutboxMessage"/> by recording it.
/// </summary>
/// <param name="state">The shared record of handled messages.</param>
public sealed class TestOutboxMessageHandler(TestOutboxHandlerState state)
    : IOutboxMessageHandler<TestOutboxMessage>
{
    /// <inheritdoc />
    public Task HandleAsync(TestOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        state.Handle(message.Text);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Delivers <see cref="OtherTestOutboxMessage"/> by recording it.
/// </summary>
/// <param name="state">The shared record of handled messages.</param>
public sealed class OtherTestOutboxMessageHandler(TestOutboxHandlerState state)
    : IOutboxMessageHandler<OtherTestOutboxMessage>
{
    /// <inheritdoc />
    public Task HandleAsync(OtherTestOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        state.Handle($"other:{message.Number}");
        return Task.CompletedTask;
    }
}
