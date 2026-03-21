using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestProgramFactoryBuilder(ITestOutputHelper testOutputHelper)
    : TestWebApplicationFactoryBuilder<Program>(testOutputHelper)
{
}
