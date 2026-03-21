using MartinDrozdik.DDD.Demo.Client.Generated.Models;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Demo.Models.Enumerations;
using MartinDrozdik.DDD.Demo.Models.ValueObjects;
using MartinDrozdik.DDD.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Demo.Tests.Invoices;

public class InvoicesTests
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public InvoicesTests(ITestOutputHelper testOutputHelper)
    {
        _factory = new DemoAppFactoryBuilder(testOutputHelper).Build();

        // Remove all invoices and persons before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        context.People.RemoveRange(context.People);
        context.Invoices.RemoveRange(context.Invoices);
        context.SaveChanges();
    }

    [Fact]
    public async Task Get_invoices()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var person = Person.Create("John Doe", DateTimeOffset.UtcNow.AddYears(-30));
        var number = InvoiceNumber.Create(2025, 1);
        var invoice = Invoice.CreateDraft(issuer: null, person, number);
        await context.AddAsync(invoice, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var client = _factory.CreateDddClient();

        // Act
        var response = await client.V1.Invoice.GetAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Contains(response.Items, e => e.Id == invoice.Id.Key);
        var responseItem = response.Items.Single(e => e.Id == invoice.Id.Key);
        Assert.Multiple(
            () => Assert.Null(responseItem.IssuerName),
            () => Assert.Equal(invoice.Recipient.FullName, responseItem.RecipientName),
            () => Assert.Equal(invoice.Number.ToString(), responseItem.InvoiceNumber),
            () => Assert.Equal(invoice.State, responseItem.State));
    }

    [Fact]
    public async Task Save_invoice_draft_without_issuer()
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

        // Act
        var response = await client.V1.Invoice.PostAsync(request, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Key);
        var responseKey = new Models.Aggregates.InvoiceId(response.Key.Value);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var invoice = await context.Invoices.SingleAsync(e => e.Id == responseKey, TestContext.Current.CancellationToken);
        Assert.NotNull(invoice);
        Assert.Multiple(
            () => Assert.Null(invoice.Issuer),
            () => Assert.Equal(request.Recipient.Name, invoice.Recipient.FullName),
            () => Assert.Equal(request.Recipient.DateOfBirth, invoice.Recipient.DateOfBirth),
            () => Assert.Equal(InvoiceState.Draft, invoice.State));
    }

    [Fact]
    public async Task Save_invoice_draft_with_issuer()
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
            Issuer = new CreateInvoiceDraftCommandRequest.CreateInvoiceDraftCommandRequest_issuer()
            {
                CreateInvoiceDraftCommandPerson = new CreateInvoiceDraftCommandPerson()
                {
                    Name = Guid.NewGuid().ToString(),
                    DateOfBirth = DateTimeOffset.UtcNow.AddYears(-40),
                },
            },
        };

        // Act
        var response = await client.V1.Invoice.PostAsync(request, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Key);
        var responseKey = new Models.Aggregates.InvoiceId(response.Key.Value);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var invoice = await context.Invoices.SingleAsync(e => e.Id == responseKey, TestContext.Current.CancellationToken);
        Assert.NotNull(invoice);
        Assert.NotNull(invoice.Issuer);
        Assert.Multiple(
            () => Assert.Equal(request.Recipient.Name, invoice.Recipient.FullName),
            () => Assert.Equal(request.Recipient.DateOfBirth, invoice.Recipient.DateOfBirth),
            () => Assert.Equal(request.Issuer.CreateInvoiceDraftCommandPerson.Name, invoice.Issuer.FullName),
            () => Assert.Equal(request.Issuer.CreateInvoiceDraftCommandPerson.DateOfBirth, invoice.Issuer.DateOfBirth),
            () => Assert.Equal(InvoiceState.Draft, invoice.State));
    }
}
