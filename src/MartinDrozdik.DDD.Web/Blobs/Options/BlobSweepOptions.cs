using MartinDrozdik.DDD.Blobs.Sweepers;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Behaviour of the <see cref="IBlobSweeper"/>, configured in code with <see cref="BlobsConfig.WithSweep"/>.
/// </summary>
/// <remarks>
/// One sweep deletes <see cref="BatchSize"/> × <see cref="MaxPasses"/> expired blobs.
/// </remarks>
public sealed class BlobSweepOptions
{
    /// <summary>
    /// Gets or sets how many expired blobs a single pass of the sweep may remove.
    /// </summary>
    /// <remarks>
    /// Too large batches may cause a single sweep to take too long and block other work, while too small batches may make the sweep take longer than necessary.
    /// In case of some buffonery, temporarily set to 1 to see what is going on.
    /// </remarks>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets how many passes of <see cref="BatchSize"/> blobs a single sweep may make.
    /// </summary>
    /// <remarks>
    /// A sweep repeats passes until no expired blob is left.
    /// Hitting this limit logs a warning that the sweep is behind, and the rest is left to the next run.
    /// </remarks>
    public int MaxPasses { get; set; } = 10;
}
