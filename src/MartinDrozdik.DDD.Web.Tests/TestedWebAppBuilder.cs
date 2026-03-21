using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestedWebAppBuilder(ITestOutputHelper testOutputHelper)
    : TestedAppBuilder<Program>(testOutputHelper)
{
}
