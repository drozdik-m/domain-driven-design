using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Results;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

/// <summary>
/// Shortcuts for tests that store a blob but are not about the two steps of an upload.
/// </summary>
internal static class BlobStorageTestExtensions
{
    /// <summary>
    /// Prepares an upload, saves the context and writes the content. The blob row is only tracked.
    /// </summary>
    /// <typeparam name="TContext">The context holding the catalogue.</typeparam>
    /// <param name="storage">The storage to upload through.</param>
    /// <param name="context">The context to track in and save.</param>
    /// <param name="request">What to store.</param>
    /// <returns>The blob, or the error of whichever step refused it.</returns>
    public static async Task<IResult<Blob, Error>> UploadAsync<TContext>(this IBlobStorage<TContext> storage, TContext context, BlobUploadRequest request)
        where TContext : DbContext
    {
        var upload = storage.PrepareUpload(context, request);
        if (!upload.IsSuccess)
        {
            return Result.Failure<Blob, Error>(upload.Error);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return await storage.AddOnSaveAsync(context, upload.Value, TestContext.Current.CancellationToken);
    }
}
