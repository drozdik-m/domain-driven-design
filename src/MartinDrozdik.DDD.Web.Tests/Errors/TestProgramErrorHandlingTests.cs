using MartinDrozdik.DDD.Testing.Errors;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<TestProgramFactoryBuilder, TestProgram>(new TestProgramFactoryBuilder(testOutputHelper))
{
}
