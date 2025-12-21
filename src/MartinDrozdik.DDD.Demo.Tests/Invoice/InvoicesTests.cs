namespace MartinDrozdik.DDD.Demo.Tests.Invoice;

public class InvoicesTests(DemoAppFactory factory)
    : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task GetInvoices()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/v1/invoice");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        //Assert.Equal("text/html; charset=utf-8",
        //    response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task GetInvoicesKiota()
    {
        // Arrange
        var client = factory.CreateDddClient();

        // Act
        var response = await client.V1.Invoice.GetAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        //Assert.Equal("text/html; charset=utf-8",
        //    response.Content.Headers.ContentType.ToString());
    }
}
