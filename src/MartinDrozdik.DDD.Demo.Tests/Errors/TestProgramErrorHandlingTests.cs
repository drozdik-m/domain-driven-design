using MartinDrozdik.DDD.Testing.Errors;

namespace MartinDrozdik.DDD.Demo.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<DemoAppFactoryBuilder, Program>(new DemoAppFactoryBuilder(testOutputHelper))
{
}
