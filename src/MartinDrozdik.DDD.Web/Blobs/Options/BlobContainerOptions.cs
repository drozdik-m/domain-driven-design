using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Errors;
using MartinDrozdik.DDD.Blobs.Signatures;

namespace MartinDrozdik.DDD.Web.Blobs.Options;

/// <summary>
/// Behaviour of one <see cref="BlobContainer"/>.
/// </summary>
/// <remarks>
/// Containers are separate by design.
/// </remarks>
public sealed class BlobContainerOptions
{
    /// <summary>
    /// Gets or sets the largest content that may be stored, in bytes, or null for no limit.
    /// </summary>
    /// <remarks>
    /// Enforced as the content streams past.
    /// It is not a substitute for the request body limit of the web server, which stops the bytes even earlier.
    /// </remarks>
    public long? MaxSize { get; set; }

    /// <summary>
    /// Gets or sets the media types that may be stored, or null to accept any.
    /// </summary>
    /// <remarks>
    /// Compared against the type the caller supplied.
    /// The library never inspects content to guess what it is; <see cref="ContentSignatures"/> checks that the content matches the declared type.
    /// </remarks>
    public IReadOnlySet<string>? AllowedContentTypes { get; set; }

    /// <summary>
    /// Gets or sets the media types that may be stored.
    /// Each uploaded file is checked against the list of signatures for its type, if any.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off by default.
    /// <see cref="ContentSignature.Common"/> covers the usual formats.
    /// To accept a format that has no signature, such as <c>text/plain</c>, list it with a lone <c>??</c>, which accepts any content of at least one byte.
    /// </para>
    /// <para>
    /// The signatures are a white-list.
    /// Only the first few bytes are compared.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<ContentSignature>? ContentSignatures { get; set; }

    /// <summary>
    /// Gets or sets the file extensions that may be stored, lowercase and without the leading dot, or null to accept any.
    /// </summary>
    /// <remarks>
    /// Once a list is set, a file that arrived without an extension is refused too.
    /// </remarks>
    public IReadOnlySet<string>? AllowedExtensions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content is hashed while it is written.
    /// </summary>
    /// <remarks>
    /// On by default. The hash costs a pass that is already being made.
    /// </remarks>
    public bool ComputeChecksum { get; set; } = true;

    /// <summary>
    /// Gets or sets how fast an upload has to commit its row before its content is removed as an orphan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The period starts at <see cref="IBlobStorage{TContext}.PrepareUpload"/>.
    /// Make it longer than your slowest upload plus the transaction that persists it.
    /// </para>
    /// <para>
    /// This is what protects an upload whose transaction has not committed yet.
    /// </para>
    /// </remarks>
    public TimeSpan OrphanGracePeriod { get; set; } = TimeSpan.FromHours(24);
}
