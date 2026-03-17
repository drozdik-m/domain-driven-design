using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Errors;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Errors;

public class TestProgramErrorHandlingTests(ITestOutputHelper testOutputHelper)
    : ErrorHandlingTests<TestWebApplicationFactoryBuilder<TestProgram>, TestProgram>(new TestWebApplicationFactoryBuilder<TestProgram>(testOutputHelper))
{
}
