using MartinDrozdik.DDD.Testing.Errors;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<DemoAppFactoryBuilder, Program>(new DemoAppFactoryBuilder(testOutputHelper))
{
}
