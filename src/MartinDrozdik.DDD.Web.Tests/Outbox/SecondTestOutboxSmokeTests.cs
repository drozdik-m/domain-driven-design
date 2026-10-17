using MartinDrozdik.DDD.Testing.Outbox;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class SecondTestOutboxSmokeTests(ITestOutputHelper testOutputHelper)
    : OutboxSmokeTests<Program, SecondTestDbContext>(new TestedWebAppBuilder(testOutputHelper))
{
}
