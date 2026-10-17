using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Testing.RecurringTasks;
using MartinDrozdik.DDD.Web.Outbox;

namespace MartinDrozdik.DDD.Demo.Tests.Outbox;

public class InvoiceOutboxDispatchTaskSmokeTests(ITestOutputHelper testOutputHelper)
    : RecurringTaskSmokeTests<Program, OutboxDispatchRecurringTask<InvoiceDbContext>>(new DemoAppBuilder(testOutputHelper))
{
}
