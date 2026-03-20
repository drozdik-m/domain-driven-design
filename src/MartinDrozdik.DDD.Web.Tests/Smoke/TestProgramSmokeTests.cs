using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<TestProgramFactoryBuilder, TestProgram>(new TestProgramFactoryBuilder(testOutputHelper))
{
}
