using MartinDrozdik.DDD.Blobs;

namespace MartinDrozdik.DDD.Demo.Models;

/// <summary>
/// The <see cref="BlobContainer"/>s of the application.
/// </summary>
public static class BlobContainers
{
    /// <summary>
    /// The container every invoice scan is stored in.
    /// </summary>
    public static readonly BlobContainer InvoiceScans = BlobContainer.Create("invoice-scans").Value;
}
