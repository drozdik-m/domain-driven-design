using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Demo.Tests.Smoke;

public class DemoAppSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<DemoAppFactoryBuilder, Program>(new DemoAppFactoryBuilder(testOutputHelper))
{
}
