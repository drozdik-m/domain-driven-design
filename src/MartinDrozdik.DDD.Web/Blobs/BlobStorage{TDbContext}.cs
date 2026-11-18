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
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Blobs;

/// <summary>
/// Coordinates the blob catalogue with the content in an <see cref="IBlobStore"/>.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the catalogue table.</typeparam>
/// <param name="store">The store holding the content.</param>
/// <param name="outbox">Delivers every removal of content: after a delete commits, or after an upload is not completed in time.</param>
/// <param name="containers">The registered containers; nothing is stored in any other.</param>
/// <param name="containerOptions">The configured behaviour of each container.</param>
/// <param name="timeProvider">Tells the time a blob was catalogued.</param>
/// <param name="logger">Records the drift that was tolerated.</param>
internal sealed class BlobStorage<TDbContext>(
    IBlobStore store,
    IOutbox<TDbContext> outbox,
    BlobContainerRegistry containers,
    IOptionsMonitor<BlobContainerOptions> containerOptions,
    TimeProvider timeProvider,
    ILogger<BlobStorage<TDbContext>> logger) : IBlobStorage<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public IResult<BlobUpload, Error> PrepareUpload(TDbContext context, BlobUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var options = GetContainerOptions(request.Container);

        // Validate content
        if (CheckAllowedContent(request.ContentType, BlobName.FromFileName(request.OriginalFileName), options) is { } refusal)
        {
            return Result.Failure<BlobUpload, Error>(refusal);
        }

        var key = BlobKey.Create(request.Container, BlobId.New());

        // Only tracked - the caller commits it, before any content is written
        // It removes the content once the grace period ends, unless AddOnSaveAsync cancels it and confirms the upload
        var cleanupSettings = new OutboxMessageSettings { AvailableAt = timeProvider.GetUtcNow() + options.OrphanGracePeriod };
        var cleanup = outbox.AddOnSave(context, DeleteBlobMessage.For(key), cleanupSettings);

        var blobUpload = new BlobUpload
        {
            Request = request,
            Key = key,
            Cleanup = cleanup,
        };
        return Result.Success<BlobUpload, Error>(blobUpload);
    }

    /// <inheritdoc />
    public async Task<IResult<Blob, Error>> AddOnSaveAsync(TDbContext context, BlobUpload upload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(upload);

        var request = upload.Request;
        var key = upload.Key;
        var options = GetContainerOptions(key.Container);

        // Validate a cleanup is not still pending insertion
        // Without a saved cleanup, an error / rollback would leave the content behind
        if (context.Set<OutboxMessage>().Local.FindEntry(upload.Cleanup.Id) is { State: EntityState.Added })
        {
            throw new InvalidOperationException($"The upload to '{key.Path}' is not saved yet. Save the context after {nameof(PrepareUpload)} and before {nameof(AddOnSaveAsync)}.");
        }

        // Write the content to the store
        // On any failure the saved cleanup removes whatever was written
        BlobWriteReceipt receipt;
        try
        {
            receipt = await store.WriteAsync(key, request.Content, new BlobWriteOptions(options.ComputeChecksum, options.MaxSize), cancellationToken);
        }
        catch (BlobTooLargeException exception)
        {
            return Result.Failure<Blob, Error>(BlobErrors.TooLarge(exception.MaxSize ?? options.MaxSize ?? 0));
        }

        // Create the blob record
        // Done before anything is tracked, so an error leaves the cleanup message in place and the content will be removed
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

        // Cancel the cleanup
        // Cancelled along insertion of the blob, so they commit or roll back together
        if (!await outbox.RemoveOnSaveAsync(context, upload.Cleanup, cancellationToken))
        {
            // The grace period ran out and a processor took the cleanup, possibly before the content was written
            // Nothing will ever point at this content, so remove it right away
            await store.DeleteAsync(key, cancellationToken);
            BlobLogging.LogUploadReclaimed(logger, key.Path);
            throw BlobUploadReclaimedException.For(key);
        }

        // Only tracked
        // The caller's save commits the blob together with the cancelled cleanup, or rolls back both
        context.Set<Blob>().Add(blob);

        return Result.Success<Blob, Error>(blob);
    }

    /// <inheritdoc />
    public async Task<IResult<Blob, Error>> GetAsync(TDbContext context, BlobId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);

        var blob = await FindBlobOrDefaultAsync(context, id, cancellationToken);

        return blob is null
            ? Result.Failure<Blob, Error>(BlobErrors.NotFound(id))
            : Result.Success<Blob, Error>(blob);
    }

    /// <inheritdoc />
    public async Task<IResult<BlobContent, Error>> OpenReadAsync(TDbContext context, BlobId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);

        // Find the blob
        var blob = await FindBlobOrDefaultAsync(context, id, cancellationToken);
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
    public Task DeleteOnSaveAsync(TDbContext context, Blob blob, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(blob);

        // Both or neither is committed
        context.Set<Blob>().Remove(blob);
        outbox.AddOnSave(context, blob.ToDeleteMessage());

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

    private static Task<Blob?> FindBlobOrDefaultAsync(TDbContext context, BlobId id, CancellationToken cancellationToken)
    => context.Set<Blob>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <summary>
    /// Reads the configured behaviour of a registered container.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <returns>Its options.</returns>
    /// <exception cref="InvalidOperationException">The container is not registered.</exception>
    private BlobContainerOptions GetContainerOptions(BlobContainer container)
    {
        if (!containers.Contains(container))
        {
            throw new InvalidOperationException($"Blob container '{container}' is not registered. Register it with {nameof(BlobsConfig)}.{nameof(BlobsConfig.WithContainer)}.");
        }

        return containerOptions.Get(container.Name);
    }
}
