using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<Program>(new TestedWebAppBuilder(testOutputHelper))
{
}
