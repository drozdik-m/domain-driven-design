using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<TestWebApplicationFactoryBuilder<TestProgram>, TestProgram>(new TestWebApplicationFactoryBuilder<TestProgram>(testOutputHelper))
{
}
