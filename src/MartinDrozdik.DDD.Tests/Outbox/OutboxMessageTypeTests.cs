using MartinDrozdik.DDD.Outbox;

namespace MartinDrozdik.DDD.Tests.Outbox;

public class OutboxMessageTypeTests
{
    [Theory]
    [InlineData("email.send.v1")]
    [InlineData("Email_Send_v1")]
    [InlineData("email-send.v2")]
    [InlineData("a")]
    public void Constructor_accepts_a_supported_key(string key)
    {
        // Arrange
        // Act
        var messageType = new OutboxMessageType(key);

        // Assert
        Assert.Equal(key, messageType.Key);
        Assert.Equal(key, messageType.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_an_empty_key(string key)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new OutboxMessageType(key));
    }

    [Fact]
    public void Constructor_rejects_a_null_key()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => new OutboxMessageType(null!));
    }

    [Fact]
    public void Constructor_rejects_a_key_longer_than_the_maximum()
    {
        // Arrange
        var key = new string('a', OutboxMessageType.MaxLength + 1);

        // Act
        var exception = Assert.Throws<ArgumentException>(() => new OutboxMessageType(key));

        // Assert
        Assert.Contains(OutboxMessageType.MaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_accepts_a_key_of_exactly_the_maximum_length()
    {
        // Arrange
        var key = new string('a', OutboxMessageType.MaxLength);

        // Act
        var messageType = new OutboxMessageType(key);

        // Assert
        Assert.Equal(OutboxMessageType.MaxLength, messageType.Key.Length);
    }

    [Theory]
    [InlineData("email send")]
    [InlineData("email:send")]
    [InlineData("email/send")]
    [InlineData("email\"send")]
    [InlineData("emailů")]
    public void Constructor_rejects_a_key_with_unsupported_characters(string key)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new OutboxMessageType(key));
    }

    [Fact]
    public void Implicit_conversion_from_string_creates_the_message_type()
    {
        // Arrange
        // Act
        OutboxMessageType messageType = "email.send.v1";

        // Assert
        Assert.Equal("email.send.v1", messageType.Key);
    }

    [Fact]
    public void Equals_successfully_returns_true_for_equal_parameters()
    {
        // Arrange
        var left = new OutboxMessageType("email.send.v1");
        var right = new OutboxMessageType("email.send.v1");

        // Act
        var equal = left.Equals(right);

        // Assert
        Assert.True(equal);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_successfully_returns_false_for_different_parameters()
    {
        // Arrange
        var left = new OutboxMessageType("email.send.v1");
        var right = new OutboxMessageType("sms.send.v1");

        // Act
        var equal = left.Equals(right);

        // Assert
        Assert.False(equal);
    }
}
