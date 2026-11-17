using System.Text.Json;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Outbox.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Dispatch;
using MartinDrozdik.DDD.Web.Outbox.Models;
using MartinDrozdik.DDD.Web.Outbox.Options;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task A_message_type_whose_members_collide_in_json_throws_an_outbox_exception()
    {
        // Arrange
        // Url and URL are both "url" under the camel case naming of the web serializer options
        using var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var exception = await Assert.ThrowsAsync<OutboxException>(() =>
            OutboxDispatcher<CollidingMessage>.DispatchAsync(services, new OutboxPayload("""{"url":"https://a.example"}"""), s_serializerOptions, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("could not be deserialized", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"Note":"a","Extra":1}""")]
    [InlineData("""{"Note":"a","Note":"b"}""")]
    [InlineData("""{"note":"a"}""")]
    [InlineData("""{"Count":"42"}""")]
    [InlineData("""{"Kind":1}""")]
    [InlineData("""{"Kind":"Third"}""")]
    public async Task The_default_serializer_options_refuse_a_payload_that_does_not_fit_its_type_exactly(string payload)
    {
        // Arrange
        // Every member is optional, so each payload breaks exactly one rule:
        // an unknown member, a duplicate, a different case, a number as a string, an enum as a number, an enum value that does not exist
        using var services = new ServiceCollection().BuildServiceProvider();
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;

        // Act
        var exception = await Assert.ThrowsAsync<OutboxException>(() =>
            OutboxDispatcher<OptionalMessage>.DispatchAsync(services, new OutboxPayload(payload), serializerOptions, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"Note":"a"}""")]
    [InlineData("""{"Text":null}""")]
    public async Task The_default_serializer_options_refuse_a_payload_without_a_non_nullable_property_outside_the_constructor(string payload)
    {
        // Arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;

        // Act
        var exception = await Assert.ThrowsAsync<OutboxException>(() =>
            OutboxDispatcher<PropertyMessage>.DispatchAsync(services, new OutboxPayload(payload), serializerOptions, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"Note":null,"Count":42,"Kind":"Second"}""")]
    public async Task The_default_serializer_options_read_a_payload_that_fits_its_type(string payload)
    {
        // Arrange
        var handler = new RecordingHandler<OptionalMessage>();
        using var services = new ServiceCollection()
            .AddSingleton<IOutboxMessageHandler<OptionalMessage>>(handler)
            .BuildServiceProvider();
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;

        // Act
        await OutboxDispatcher<OptionalMessage>.DispatchAsync(services, new OutboxPayload(payload), serializerOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(handler.Handled);
    }

    [Fact]
    public async Task The_default_serializer_options_read_a_payload_without_a_nullable_property_outside_the_constructor()
    {
        // Arrange
        var handler = new RecordingHandler<PropertyMessage>();
        using var services = new ServiceCollection()
            .AddSingleton<IOutboxMessageHandler<PropertyMessage>>(handler)
            .BuildServiceProvider();
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;

        // Act
        await OutboxDispatcher<PropertyMessage>.DispatchAsync(services, new OutboxPayload("""{"Text":"a"}"""), serializerOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("a", handler.Handled?.Text);
        Assert.Null(handler.Handled?.Note);
    }

    [Fact]
    public async Task The_default_serializer_options_store_member_and_enum_names_and_read_them_back()
    {
        // Arrange
        var handler = new RecordingHandler<OptionalMessage>();
        using var services = new ServiceCollection()
            .AddSingleton<IOutboxMessageHandler<OptionalMessage>>(handler)
            .BuildServiceProvider();
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;
        var message = new OptionalMessage("a", 42, OptionalKind.Second);

        // Act
        var payload = JsonSerializer.Serialize(message, serializerOptions);
        await OutboxDispatcher<OptionalMessage>.DispatchAsync(services, new OutboxPayload(payload), serializerOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("""{"Note":"a","Count":42,"Kind":"Second"}""", payload);
        Assert.Equal(message, handler.Handled);
    }

    [Fact]
    public void The_default_serializer_options_refuse_to_store_an_enum_value_the_enum_does_not_define()
    {
        // Arrange
        var serializerOptions = new OutboxOptions<DbContext>().SerializerOptions;

        // Act
        var exception = Record.Exception(() => JsonSerializer.Serialize(new OptionalMessage(Kind: (OptionalKind)7), serializerOptions));

        // Assert
        // The outbox turns a JsonException into an OutboxException on enqueue
        Assert.IsAssignableFrom<JsonException>(exception);
    }

    private sealed record ReadableMessage(string Text) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "dispatcher.readable.v1";
    }

    private sealed record CollidingMessage(string Url, string URL) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "dispatcher.colliding.v1";
    }

    private sealed record OptionalMessage(string? Note = null, int Count = 0, OptionalKind Kind = OptionalKind.First) : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "dispatcher.optional.v1";
    }

    private enum OptionalKind
    {
        First,
        Second,
    }

    private sealed class PropertyMessage : IOutboxMessage
    {
        public static OutboxMessageType MessageType => "dispatcher.property.v1";

        public string Text { get; init; } = string.Empty;

        public string? Note { get; init; }
    }

    private sealed class RecordingHandler<TMessage> : IOutboxMessageHandler<TMessage>
        where TMessage : IOutboxMessage
    {
        public TMessage? Handled { get; private set; }

        public Task HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            Handled = message;
            return Task.CompletedTask;
        }
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
