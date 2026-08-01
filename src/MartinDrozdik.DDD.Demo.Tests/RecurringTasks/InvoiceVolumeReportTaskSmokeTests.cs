using MartinDrozdik.DDD.Demo.RecurringTasks;
using MartinDrozdik.DDD.Testing.RecurringTasks;

namespace MartinDrozdik.DDD.Demo.Tests.RecurringTasks;

public class InvoiceVolumeReportTaskSmokeTests(ITestOutputHelper testOutputHelper)
    : RecurringTaskSmokeTests<Program, InvoiceVolumeReportTask>(new DemoAppBuilder(testOutputHelper))
{
}
