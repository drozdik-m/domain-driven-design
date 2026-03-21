using MartinDrozdik.DDD.Testing.Errors;

namespace MartinDrozdik.DDD.Web.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<TestedWebAppBuilder, Program>(new TestedWebAppBuilder(testOutputHelper))
{
}
