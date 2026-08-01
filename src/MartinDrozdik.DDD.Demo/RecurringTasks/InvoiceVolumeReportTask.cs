using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Demo.RecurringTasks;

/// <summary>
/// Reports how many invoices are piling up, and complains once there are too many of them.
/// </summary>
/// <remarks>
/// Note what this class does <i>not</i> have to do: no timer, no loop, no try/catch to stay alive,
/// no manual scope for the <see cref="InvoiceDbContext"/>. It is resolved from a fresh scope for
/// every iteration, so scoped services are injected like in any other handler.
/// <para>
/// The schedule lives in <c>Program.cs</c>. Anything the task itself needs — such as
/// <see cref="WarnAboveCount"/> — is the task's own business; make it an options class of your
/// own if it has to differ per environment.
/// </para>
/// </remarks>
public class InvoiceVolumeReportTask(
    InvoiceDbContext context,
    TimeProvider timeProvider,
    ILogger<InvoiceVolumeReportTask> logger) : IRecurringTask
{
    /// <summary>
    /// The number of invoices above which the report starts complaining.
    /// </summary>
    private const int WarnAboveCount = 100;

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var invoices = await context.Invoices.CountAsync(cancellationToken);
        var reportedAt = timeProvider.GetUtcNow();

        if (invoices > WarnAboveCount)
        {
            logger.LogWarning(
                "There are {Invoices} invoices as of {ReportedAt}, more than the {WarnAboveCount} we are happy with.",
                invoices,
                reportedAt,
                WarnAboveCount);
            return;
        }

        logger.LogInformation("There are {Invoices} invoices as of {ReportedAt}. Nothing to worry about.", invoices, reportedAt);
    }
}
