using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Web.Outbox;

namespace MartinDrozdik.DDD.Web.Blobs.Outbox;

/// <summary>
/// Extensions for <see cref="OutboxConfig"/>.
/// </summary>
public static class OutboxConfigExtensions
{
    /// <summary>
    /// Registers the message type blob storage delivers through the outbox.
    /// </summary>
    /// <param name="config">The <see cref="OutboxConfig"/> to extend.</param>
    /// <returns>The <see cref="OutboxConfig"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddOutbox&lt;InvoiceDbContext&gt;(
    ///     options =&gt; options.Retention = TimeSpan.FromDays(7),
    ///     config =&gt; config
    ///         .WithMessage&lt;InvoiceDraftedMessage, InvoiceDraftedMessageHandler&gt;()
    ///         .WithBlobs());
    /// </code>
    /// </example>
    public static OutboxConfig WithBlobs(this OutboxConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.WithMessage<DeleteBlobMessage, DeleteBlobMessageHandler>();
    }
}
