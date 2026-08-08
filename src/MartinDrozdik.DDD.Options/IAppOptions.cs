namespace MartinDrozdik.DDD.Options;

/// <summary>
/// Represents application options with a predetermined <see cref="Section"/>.
/// </summary>
public interface IAppOptions
{
    /// <summary>
    /// Gets the full identifier of the section where options are located.
    /// </summary>
    /// <example>Application:Database.</example>
    static abstract string Section { get; }
}
