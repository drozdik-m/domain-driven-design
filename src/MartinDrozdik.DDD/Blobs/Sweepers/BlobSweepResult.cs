namespace MartinDrozdik.DDD.Blobs.Sweepers;

/// <summary>
/// What one sweep removed.
/// </summary>
/// <param name="Expired">Blobs removed because they had passed their expiry.</param>
public sealed record BlobSweepResult(int Expired)
{
    /// <summary>
    /// A sweep that found nothing to do.
    /// </summary>
    public static readonly BlobSweepResult Empty = new(0);
}
