namespace MartinDrozdik.DDD.Blobs.Sweepers;

/// <summary>
/// Removes blobs that have expired.
/// </summary>
/// <remarks>
/// <para>
/// The sweep only finds what has expired and hands it to the outbox, exactly as <see cref="IBlobStorage.DeleteOnSaveAsync"/>.
/// </para>
/// </remarks>
public interface IBlobSweeper
{
    /// <summary>
    /// Runs one sweep, removing expired blobs batch by batch until none is left or the configured number of passes is reached.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the application is shutting down.</param>
    /// <returns>What the sweep removed.</returns>
    Task<BlobSweepResult> SweepAsync(CancellationToken cancellationToken);
}
