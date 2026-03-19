using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Smoke;

public class DemoAppSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<TestWebApplicationFactoryBuilder<Program>, Program>(new TestWebApplicationFactoryBuilder<Program>(testOutputHelper))
{
}
