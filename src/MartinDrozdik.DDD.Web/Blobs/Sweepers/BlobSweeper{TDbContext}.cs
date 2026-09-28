using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Sweepers;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs.Sweepers;

/// <summary>
/// Removes the blobs in <typeparamref name="TDbContext"/> whose expiry has passed.
/// </summary>
/// <remarks>
/// Works in passes of at most <see cref="BlobSweepOptions.BatchSize"/> rows, each removed via outbox in one commit,
/// until no expired row is left or <see cref="BlobSweepOptions.MaxPasses"/> is reached.
/// Sweeps every container.
/// </remarks>
/// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
/// <param name="context">The context holding the catalogue.</param>
/// <param name="outbox">Removes the content once the rows are gone.</param>
/// <param name="sweepOptions">The configured behaviour of the sweep.</param>
/// <param name="timeProvider">Decides what counts as expired.</param>
/// <param name="logger">Records what was removed.</param>
internal sealed class BlobSweeper<TDbContext>(
    TDbContext context,
    IOutbox<TDbContext> outbox,
    IOptions<BlobSweepOptions> sweepOptions,
    TimeProvider timeProvider,
    ILogger<BlobSweeper<TDbContext>> logger) : IBlobSweeper
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<BlobSweepResult> SweepAsync(CancellationToken cancellationToken)
    {
        var options = sweepOptions.Value;
        var removed = 0;
        var caughtUp = false;

        // Sweep until no expired row is left or the configured number of passes is reached
        for (var pass = 0; pass < options.MaxPasses && !caughtUp; pass++)
        {
            var (loaded, removedInPass) = await SweepPassAsync(cancellationToken);
            removed += removedInPass;

            // A page shorter than a batch means no expired row was left behind it
            caughtUp = loaded < options.BatchSize;
        }

        if (removed > 0)
        {
            BlobLogging.LogSwept(logger, removed);
        }

        if (!caughtUp)
        {
            BlobLogging.LogSweepBehind(logger, options.MaxPasses, options.BatchSize);
        }

        return new BlobSweepResult(removed);
    }

    /// <summary>
    /// Removes one batch of expired rows.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>How many expired rows were loaded, and how many of them were removed.</returns>
    private async Task<(int Loaded, int Removed)> SweepPassAsync(CancellationToken cancellationToken)
    {
        var expired = await LoadExpiredAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return (0, 0);
        }

        var removals = expired.ConvertAll(ScheduleRemoval);
        await TrySaveRemovalsAsync(removals, cancellationToken);
        return (expired.Count, removals.Count);
    }

    /// <summary>
    /// Loads the oldest expired rows, at most one batch of them.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>The expired rows, the longest expired first.</returns>
    private Task<List<Blob>> LoadExpiredAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime;

        return context.Set<Blob>()
            .Where(b => b.ExpiresAt != null && b.ExpiresAt <= cutoff)
            .OrderBy(b => b.ExpiresAt)
            .Take(sweepOptions.Value.BatchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tracks the removal of the row, together with the message removing its content.
    /// </summary>
    /// <param name="blob">The expired row.</param>
    /// <returns>The row with the ticket of its message.</returns>
    private Removal ScheduleRemoval(Blob blob)
    {
        context.Set<Blob>().Remove(blob);
        var deleteMessage = blob.ToDeleteMessage();
        var outboxTicket = outbox.AddOnSave(deleteMessage);
        return new Removal(blob, outboxTicket);
    }

    /// <summary>
    /// Tries to save the removals.
    /// Ignoring concurrent conflicts if any occured.
    /// </summary>
    /// <remarks>
    /// Another sweep, or a delete on another instance, can pick the same expired rows, and the metadata or expiry of a row
    /// can change after it was loaded. Such rows are let go; one that is still expired is picked up again by a later pass.
    /// A conflict on anything else is not the sweep's to resolve, so it is rethrown.
    /// </remarks>
    /// <param name="removals">The removed rows; those someone else had already removed or changed are taken out of it.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <exception cref="DbUpdateConcurrencyException">A conflict on a row the sweep did not remove.</exception>
    private async Task TrySaveRemovalsAsync(List<Removal> removals, CancellationToken cancellationToken)
    {
        var alreadyRemoved = 0;

        var maxSaves = removals.Count;
        for (var save = 0; save <= maxSaves; save++)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                var takenBack = 0;
                foreach (var entry in exception.Entries)
                {
                    if (await TakeBackAsync(removals, entry, cancellationToken))
                    {
                        takenBack++;
                    }
                }

                // Retrying without taking anything back would fail the same way
                if (takenBack == 0)
                {
                    throw;
                }

                alreadyRemoved += takenBack;
            }
        }

        if (alreadyRemoved > 0)
        {
            BlobLogging.LogExpiredAlreadyRemoved(logger, alreadyRemoved);
        }
    }

    /// <summary>
    /// When the failed entry is one of the removed rows, stops tracking it and takes back its outbox message too.
    /// </summary>
    /// <remarks>
    /// Any other entry is left alone, it belongs to whoever tracked it.
    /// </remarks>
    /// <param name="removals">The removed rows; the taken back one is taken out of it.</param>
    /// <param name="entry">The entry that affected no row.</param>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns><see langword="true"/> when the entry was one of the removed rows.</returns>
    private async Task<bool> TakeBackAsync(List<Removal> removals, EntityEntry entry, CancellationToken cancellationToken)
    {
        // Find the removal
        var index = removals.FindIndex(r => ReferenceEquals(r.Blob, entry.Entity));
        if (index < 0)
        {
            return false;
        }

        // Take back blob removal
        entry.State = EntityState.Detached;

        // Take back the outbox
        if (!await outbox.RemoveOnSaveAsync(removals[index].Ticket, cancellationToken))
        {
            throw new InvalidOperationException($"Could not take back the outbox message for blob {removals[index].Blob.Id}.");
        }

        removals.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// An expired row tracked for removal, with the outbox message removing its content.
    /// </summary>
    /// <param name="Blob">The removed row.</param>
    /// <param name="Ticket">Identifies the message, so it can be taken back.</param>
    private sealed record Removal(Blob Blob, OutboxTicket Ticket);
}
