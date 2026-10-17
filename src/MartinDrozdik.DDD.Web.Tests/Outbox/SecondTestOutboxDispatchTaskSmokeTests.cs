using MartinDrozdik.DDD.Testing.RecurringTasks;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Outbox;

public class SecondTestOutboxDispatchTaskSmokeTests(ITestOutputHelper testOutputHelper)
    : RecurringTaskSmokeTests<Program, OutboxDispatchRecurringTask<SecondTestDbContext>>(new TestedWebAppBuilder(testOutputHelper))
{
}
