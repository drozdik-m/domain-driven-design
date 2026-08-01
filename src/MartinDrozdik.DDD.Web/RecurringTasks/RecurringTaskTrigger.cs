using System.Threading.Channels;

namespace MartinDrozdik.DDD.Web.RecurringTasks;

/// <summary>
/// Default <see cref="IRecurringTaskTrigger{TTask}"/>, backed by a single-slot <see cref="Channel{T}"/>.
/// Registered as a singleton so producers and the hosted loop share one instance.
/// </summary>
/// <typeparam name="TTask">The task this trigger belongs to.</typeparam>
internal sealed class RecurringTaskTrigger<TTask> : IRecurringTaskTrigger<TTask>
    where TTask : IRecurringTask
{
    /// <summary>
    /// <see cref="BoundedChannelFullMode.DropWrite"/> + 1 slot -> makes simple coalesce trigger.
    /// Byte channel for small arbitrary flag.
    /// </summary>
    private readonly Channel<byte> _requests = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    /// <inheritdoc />
    public void Trigger()
    {
        // TryWrite never blocks and never throws
        _requests.Writer.TryWrite(default);
    }

    /// <summary>
    /// Waits until a trigger is requested, consuming it.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when waiting should stop, typically once the period elapsed.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when a request has been consumed.</returns>
    internal async ValueTask WaitAsync(CancellationToken cancellationToken)
    {
        await _requests.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }
}
