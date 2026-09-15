using MartinDrozdik.DDD.Demo.Client.Generated.Models;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Outbox;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Demo.Tests.Outbox;

/// <summary>
/// Exercises the outbox end to end through the real API of the demo application.
/// </summary>
public class InvoiceOutboxTests
{
    private readonly TestedApp<Program> _factory;

    public InvoiceOutboxTests(ITestOutputHelper testOutputHelper)
    {
        _factory = new DemoAppBuilder(testOutputHelper).Build();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        context.People.RemoveRange(context.People);
        context.Invoices.RemoveRange(context.Invoices);
        context.Set<OutboxMessage>().RemoveRange(context.Set<OutboxMessage>());
        context.SaveChanges();
    }

    [Fact]
    public async Task Creating_an_invoice_draft_enqueues_a_message_in_the_same_transaction()
    {
        // Arrange
        var client = _factory.CreateDddClient();
        var recipientName = Guid.NewGuid().ToString();
        var request = new CreateInvoiceDraftCommandRequest()
        {
            Recipient = new CreateInvoiceDraftCommandPerson()
            {
                Name = recipientName,
                DateOfBirth = DateTimeOffset.UtcNow.AddYears(-30),
            },
            Issuer = null,
        };

        // Act
        var response = await client.V1.Invoice.PostAsync(request, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response?.Key);
        var message = await GetSingleMessageAsync();
        Assert.Equal(InvoiceDraftedMessage.MessageType, message.MessageType);
        Assert.Contains(recipientName, message.Payload.Value, StringComparison.Ordinal);
        Assert.Contains(response.Key.Value.ToString(), message.Payload.Value, StringComparison.OrdinalIgnoreCase);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public async Task Processing_the_outbox_delivers_the_enqueued_message()
    {
        // Arrange
        var client = _factory.CreateDddClient();
        var request = new CreateInvoiceDraftCommandRequest()
        {
            Recipient = new CreateInvoiceDraftCommandPerson()
            {
                Name = Guid.NewGuid().ToString(),
                DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
            },
            Issuer = null,
        };
        await client.V1.Invoice.PostAsync(request, cancellationToken: CancellationToken.None);

        // Act
        var dispatched = await _factory.ProcessOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, dispatched);
        var message = await GetSingleMessageAsync();
        Assert.NotNull(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.LastError);
    }

    private async Task<OutboxMessage> GetSingleMessageAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        return await context.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
    }
}
