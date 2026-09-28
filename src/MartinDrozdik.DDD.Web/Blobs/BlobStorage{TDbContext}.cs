using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Blobs.Exceptions;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Outbox;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Outbox;
using MartinDrozdik.DDD.Results;
using MartinDrozdik.DDD.Web.Blobs.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Coordinates the blob catalogue in <typeparamref name="TDbContext"/> with the content in an <see cref="IBlobStore"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
/// <param name="context">The context holding the catalogue.</param>
/// <param name="store">The store holding the content.</param>
/// <param name="outbox">Delivers every removal of content: after a delete commits, or after an upload fails to.</param>
/// <param name="containers">The registered containers; nothing is stored in any other.</param>
/// <param name="containerOptions">The configured behaviour of each container.</param>
/// <param name="timeProvider">Tells the time a blob was catalogued.</param>
/// <param name="logger">Records the drift that was tolerated.</param>
internal sealed class BlobStorage<TDbContext>(
    TDbContext context,
    IBlobStore store,
    IOutbox<TDbContext> outbox,
    BlobContainerRegistry containers,
    IOptionsMonitor<BlobContainerOptions> containerOptions,
    TimeProvider timeProvider,
    ILogger<BlobStorage<TDbContext>> logger) : IBlobStorage
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public async Task<IResult<Blob, Error>> AddOnSaveAsync(BlobUploadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Missing container configuration
        if (!containers.Contains(request.Container))
        {
            throw new InvalidOperationException($"Blob container '{request.Container}' is not registered. Register it with {nameof(BlobsConfig)}.{nameof(BlobsConfig.WithContainer)}.");
        }

        var options = containerOptions.Get(request.Container.Name);

        // Validate content
        if (CheckAllowedContent(request.ContentType, BlobName.FromFileName(request.OriginalFileName), options) is { } refusal)
        {
            return Result.Failure<Blob, Error>(refusal);
        }

        var key = BlobKey.Create(request.Container, BlobId.New());
        var writeOptions = new BlobWriteOptions(options.ComputeChecksum, options.MaxSize);

        // Immediately schedule content removal
        // Cleans up content in case of a rollback/failure after the grace period
        var graceRemovalMessageSettings = new OutboxMessageSettings { AvailableAt = GetEndOfGracePeriod(options) };
        var graceRemovalTicket = await outbox.AddNowAsync(DeleteBlobMessage.For(key), graceRemovalMessageSettings, cancellationToken);

        // Write the content to the store
        BlobWriteReceipt receipt;
        try
        {
            receipt = await store.WriteAsync(key, request.Content, writeOptions, cancellationToken);
        }
        catch (BlobTooLargeException exception)
        {
            return Result.Failure<Blob, Error>(BlobErrors.TooLarge(exception.MaxSize ?? options.MaxSize ?? 0));
        }

        // Postponed once the content is saved, so the grace period counts from here rather than from when the upload started
        var postponedTicket = await outbox.PostponeNowAsync(graceRemovalTicket, GetEndOfGracePeriod(options), cancellationToken);

        // Check if the removal message was already processed
        // The grace period probably expired before the content was saved or the outbox was unable to postpone the removal for some reason (e.g. concurrency issues)
        if (postponedTicket is null)
        {
            // Schedule content removal to be absolutely sure about cleanup
            await outbox.AddNowAsync(DeleteBlobMessage.For(key), OutboxMessageSettings.Default, cancellationToken);
            BlobLogging.LogUploadReclaimed(logger, key.Path);
            throw BlobUploadReclaimedException.For(key);
        }

        // Create the blob record before anything is tracked, so an error leaves the grace removal in place
        var blob = Blob.Create(new CreateBlobParams
        {
            Key = key,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            Size = receipt.Size,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            Checksum = receipt.Checksum,
            Metadata = request.Metadata,
            CreatedBy = request.CreatedBy,
            ExpiresAtUtc = request.ExpiresAt?.UtcDateTime,
        });

        // Start tracking removal of the scheduled outbox removal message
        // Grace removal is stopped on save along atomic insert of the blob
        var stopGraceRemovalScheduled = await outbox.RemoveOnSaveAsync(postponedTicket.Value, cancellationToken);
        if (!stopGraceRemovalScheduled)
        {
            // Schedule content removal to be absolutely sure about cleanup
            await outbox.AddNowAsync(DeleteBlobMessage.For(key), OutboxMessageSettings.Default, cancellationToken);
            BlobLogging.LogUploadReclaimed(logger, key.Path);
            throw BlobUploadReclaimedException.For(key);
        }

        // Only tracked
        // The caller's own SaveChanges commits the blob together with removal of the grace period message, or rolls back both
        context.Set<Blob>().Add(blob);

        return Result.Success<Blob, Error>(blob);
    }

    /// <inheritdoc />
    public async Task<IResult<Blob, Error>> GetAsync(BlobId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        var blob = await FindBlobOrDefaultAsync(id, cancellationToken);

        return blob is null
            ? Result.Failure<Blob, Error>(BlobErrors.NotFound(id))
            : Result.Success<Blob, Error>(blob);
    }

    /// <inheritdoc />
    public async Task<IResult<BlobContent, Error>> OpenReadAsync(BlobId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        // Find the blob
        var blob = await FindBlobOrDefaultAsync(id, cancellationToken);
        if (blob is null)
        {
            return Result.Failure<BlobContent, Error>(BlobErrors.NotFound(id));
        }

        // Open the content
        var content = await store.OpenReadAsync(blob.Key, cancellationToken);
        if (content is null)
        {
            // Blob exists but content is missing
            BlobLogging.LogContentMissing(logger, blob.Key.Path);
            return Result.Failure<BlobContent, Error>(BlobErrors.ContentMissing(blob.Key));
        }

        return Result.Success<BlobContent, Error>(new BlobContent(blob, content));
    }

    /// <inheritdoc />
    public Task DeleteOnSaveAsync(Blob blob, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blob);

        // Both or neither is committed
        context.Set<Blob>().Remove(blob);
        outbox.AddOnSave(blob.ToDeleteMessage());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Decides whether content may be stored at all.
    /// </summary>
    /// <param name="contentType">The media type the content was declared as.</param>
    /// <param name="name">The name it would be catalogued under.</param>
    /// <param name="options">The configured behaviour of the container.</param>
    /// <returns>The reason to refuse, or null to accept.</returns>
    private static Error? CheckAllowedContent(MediaType contentType, BlobName name, BlobContainerOptions options)
    {
        // Validate mime type
        if (options.AllowedContentTypes is { } allowedContentTypes && !allowedContentTypes.Contains(contentType.Value))
        {
            return BlobErrors.ContentTypeNotAllowed(contentType);
        }

        // Validate extension
        if (options.AllowedExtensions is { } allowedExtensions &&
            (name.Extension is null || !allowedExtensions.Contains(name.Extension)))
        {
            return BlobErrors.ExtensionNotAllowed(name.Extension);
        }

        return null;
    }

    private DateTimeOffset GetEndOfGracePeriod(BlobContainerOptions options)
        => timeProvider.GetUtcNow() + options.OrphanGracePeriod;

    private Task<Blob?> FindBlobOrDefaultAsync(BlobId id, CancellationToken cancellationToken)
        => context.Set<Blob>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}
