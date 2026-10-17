using System.Text.Json;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Dispatch;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

/// <summary>
/// Verifies how a stored payload is read back into its message before the handler gets it.
/// </summary>
public class OutboxDispatcherTests
{
    private static readonly JsonSerializerOptions s_serializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task A_payload_that_is_not_json_throws_an_outbox_exception()
    {
        // Arrange
        using var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var exception = await Assert.ThrowsAsync<OutboxException>(() =>
            OutboxDispatcher<ReadableMessage>.DispatchAsync(services, new OutboxPayload("not json at all"), s_serializerOptions, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
        Assert.Contains("could not be deserialized", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_message_type_the_serializer_cannot_create_throws_an_outbox_exception()
    {
        // Arrange
        // It serializes fine, but without a constructor the serializer can pick it cannot be read back
        using var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var exception = await Assert.ThrowsAsync<OutboxException>(() =>
            OutboxDispatcher<UncreatableMessage>.DispatchAsync(services, new OutboxPayload("""{"text":"hello"}"""), s_serializerOptions, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("could not be deserialized", exception.Message, StringComparison.Ordinal);
    }

    private sealed record ReadableMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "dispatcher.readable.v1";
    }

#pragma warning disable S1144 // Unused private constructors - two of them leave the serializer no constructor to pick
    private sealed class UncreatableMessage : IOutboxMessage
    {
        public UncreatableMessage(string text)
        {
            Text = text;
        }

        public UncreatableMessage(int number)
        {
            Text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static OutboxMessageType MessageType => "dispatcher.uncreatable.v1";

        public string Text { get; }
    }
#pragma warning restore S1144
}
