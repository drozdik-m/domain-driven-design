namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// The blob containers the test application registers.
/// </summary>
public static class TestBlobContainers
{
    /// <summary>
    /// The container every blob test stores into.
    /// </summary>
    public const string Invoices = "invoices";

    /// <summary>
    /// A container with a size limit of its own, to show it applies to no other container.
    /// </summary>
    public const string Avatars = "avatars";

    /// <summary>
    /// The size limit of <see cref="Avatars"/>, in bytes.
    /// </summary>
    public const long AvatarMaxSize = 4;
}
