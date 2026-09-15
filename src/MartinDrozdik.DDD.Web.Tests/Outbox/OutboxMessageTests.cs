using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web.Outbox.Models;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class OutboxMessageTests
{
    private static readonly DateTime s_now = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_makes_a_message_that_is_immediately_deliverable()
    {
        // Arrange
        // Act
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);

        // Assert
        Assert.Equal("email.send.v1", message.MessageType.Key);
        Assert.Equal("{}", message.Payload.Value);
        Assert.Equal(s_now, message.OccurredAt);
        Assert.Equal(s_now, message.AvailableAt);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedUntil);
        Assert.Null(message.LastError);
        Assert.Equal(0, message.Attempts);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Claim_takes_a_lease_that_expires_after_the_given_duration()
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);
        var claimId = Guid.CreateVersion7();

        // Act
        message.Claim(claimId, s_now, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(claimId, message.ClaimedBy);
        Assert.Equal(s_now.AddMinutes(5), message.ClaimedUntil);
    }

    [Fact]
    public void MarkProcessed_completes_the_message_and_releases_the_claim()
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);
        message.Claim(Guid.CreateVersion7(), s_now, TimeSpan.FromMinutes(5));

        // Act
        message.MarkProcessed(s_now.AddSeconds(1));

        // Assert
        Assert.Equal(s_now.AddSeconds(1), message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.FailedAt);
        Assert.Null(message.ClaimedBy);
        Assert.Null(message.ClaimedUntil);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void MarkRetrying_pushes_availability_forward_and_records_the_error()
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);
        message.Claim(Guid.CreateVersion7(), s_now, TimeSpan.FromMinutes(5));

        // Act
        message.MarkRetrying(s_now.AddSeconds(1), TimeSpan.FromSeconds(10), "Boom");

        // Assert
        Assert.Equal(s_now.AddSeconds(11), message.AvailableAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("Boom", message.LastError);
        Assert.Null(message.ProcessedAt);
        Assert.Null(message.FailedAt);
        Assert.Null(message.ClaimedBy);
    }

    [Fact]
    public void MarkFailed_dead_letters_the_message_and_records_the_error()
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);

        // Act
        message.MarkFailed(s_now.AddSeconds(1), "Boom");

        // Assert
        Assert.Equal(s_now.AddSeconds(1), message.FailedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("Boom", message.LastError);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void MarkFailed_truncates_an_error_longer_than_the_maximum()
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);
        var error = new string('e', OutboxMessage.LastErrorMaxLength + 500);

        // Act
        message.MarkFailed(s_now, error);

        // Assert
        Assert.Equal(OutboxMessage.LastErrorMaxLength, message.LastError?.Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Lifecycle_methods_throw_for_a_message_in_a_terminal_state(bool processed)
    {
        // Arrange
        var message = OutboxMessage.Create("email.send.v1", "{}", s_now);
        if (processed)
        {
            message.MarkProcessed(s_now);
        }
        else
        {
            message.MarkFailed(s_now, "Boom");
        }

        // Act
        // Assert
        Assert.Throws<BusinessRuleValidationException>(() => message.Claim(Guid.CreateVersion7(), s_now, TimeSpan.FromMinutes(5)));
        Assert.Throws<BusinessRuleValidationException>(() => message.MarkProcessed(s_now));
        Assert.Throws<BusinessRuleValidationException>(() => message.MarkRetrying(s_now, TimeSpan.FromSeconds(1), "Boom"));
        Assert.Throws<BusinessRuleValidationException>(() => message.MarkFailed(s_now, "Boom"));
    }

    [Fact]
    public void Every_lifecycle_method_rotates_the_concurrency_stamp()
    {
        // Arrange
        var claimed = OutboxMessage.Create("email.send.v1", "{}", s_now);
        var processed = OutboxMessage.Create("email.send.v1", "{}", s_now);
        var retrying = OutboxMessage.Create("email.send.v1", "{}", s_now);
        var failed = OutboxMessage.Create("email.send.v1", "{}", s_now);

        var claimedStamp = claimed.ConcurrencyStamp;
        var processedStamp = processed.ConcurrencyStamp;
        var retryingStamp = retrying.ConcurrencyStamp;
        var failedStamp = failed.ConcurrencyStamp;

        // Act
        claimed.Claim(Guid.CreateVersion7(), s_now, TimeSpan.FromMinutes(5));
        processed.MarkProcessed(s_now);
        retrying.MarkRetrying(s_now, TimeSpan.FromSeconds(10), "Boom");
        failed.MarkFailed(s_now, "Boom");

        // Assert
        Assert.NotEqual(claimedStamp, claimed.ConcurrencyStamp);
        Assert.NotEqual(processedStamp, processed.ConcurrencyStamp);
        Assert.NotEqual(retryingStamp, retrying.ConcurrencyStamp);
        Assert.NotEqual(failedStamp, failed.ConcurrencyStamp);
    }
}
