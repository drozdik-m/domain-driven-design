using MartinDrozdik.DDD.Testing.Errors;

namespace MartinDrozdik.DDD.Web.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<TestProgramFactoryBuilder, Program>(new TestProgramFactoryBuilder(testOutputHelper))
{
}
