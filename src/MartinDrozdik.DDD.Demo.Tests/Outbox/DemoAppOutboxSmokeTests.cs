using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Testing.Outbox;

namespace MartinDrozdik.DDD.Demo.Tests.Outbox;

public class DemoAppOutboxSmokeTests(ITestOutputHelper testOutputHelper)
    : OutboxSmokeTests<Program, InvoiceDbContext>(new DemoAppBuilder(testOutputHelper))
{
}
