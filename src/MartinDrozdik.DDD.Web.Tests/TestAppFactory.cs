using MartinDrozdik.DDD.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestAppFactory : TestWebApplicationFactory<TestProgram>
{
    public TestAppFactory(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    public TestAppFactory(ITestOutputHelper testOutputHelper, Action<IWebHostBuilder> config)
        : base(testOutputHelper, config)
    {
    }
}
