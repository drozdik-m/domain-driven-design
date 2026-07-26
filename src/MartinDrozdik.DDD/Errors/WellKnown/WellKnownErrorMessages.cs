namespace MartinDrozdik.DDD.Errors.WellKnown;

/// <summary>
/// English messages used by the well-known <see cref="Error"/>s.
/// </summary>
internal static class WellKnownErrorMessages
{
    /// <summary>
    /// Message of the <see cref="ErrorCodes.NotFound"/> error.
    /// </summary>
    public const string NotFound = "The requested item was not found.";

    /// <summary>
    /// Message of the <see cref="ErrorCodes.AlreadyExists"/> error.
    /// </summary>
    public const string AlreadyExists = "The item already exists.";

    /// <summary>
    /// Message of the <see cref="ErrorCodes.InvalidObject"/> error caused by a single violated invariant.
    /// </summary>
    public const string InvariantError = "An invariant has been violated.";

    /// <summary>
    /// Message of the <see cref="ErrorCodes.InvalidObject"/> error caused by multiple violated invariants.
    /// </summary>
    public const string InvariantErrors = "Invariants have been violated.";
}
