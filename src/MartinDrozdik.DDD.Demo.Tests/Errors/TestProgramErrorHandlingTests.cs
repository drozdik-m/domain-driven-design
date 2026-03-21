using MartinDrozdik.DDD.Testing.Errors;

namespace MartinDrozdik.DDD.Demo.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<DemoAppBuilder, Program>(new DemoAppBuilder(testOutputHelper))
{
}
