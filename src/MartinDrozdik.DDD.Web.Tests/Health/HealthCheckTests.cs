using System.Net;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Health;

public class HealthCheckTests(ITestOutputHelper testOutputHelper)
{
    private readonly TestAppFactory _factory = new(testOutputHelper);

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Get_healthy_result(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.CacheControl);
    }
}
