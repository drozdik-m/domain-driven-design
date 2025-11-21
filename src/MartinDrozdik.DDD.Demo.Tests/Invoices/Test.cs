using Microsoft.AspNetCore.Mvc.Testing;

namespace MartinDrozdik.DDD.Demo.Tests.Invoices;

public class Test(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SampleTest()
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
}
