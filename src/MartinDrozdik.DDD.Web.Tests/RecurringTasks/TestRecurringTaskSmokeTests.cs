using MartinDrozdik.DDD.Testing.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.RecurringTasks;

public class TestRecurringTaskSmokeTests(ITestOutputHelper testOutputHelper)
    : RecurringTaskSmokeTests<Program, TestRecurringTask>(new TestedWebAppBuilder(testOutputHelper))
{
}
