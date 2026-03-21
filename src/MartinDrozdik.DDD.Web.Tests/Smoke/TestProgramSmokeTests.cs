using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<TestProgramFactoryBuilder, Program>(new TestProgramFactoryBuilder(testOutputHelper))
{
}
