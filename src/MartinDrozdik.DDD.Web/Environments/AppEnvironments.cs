namespace MartinDrozdik.DDD.Web.Environments;

/// <summary>
/// List of application environment names used in the project besides the standard ones.
/// </summary>
public static class AppEnvironments
{
    /// <summary>
    /// Specifies the Testing environment.
    /// </summary>
    /// <remarks>The testing environment can enable features that shouldn't be exposed in production.</remarks>
    public static readonly string Testing = "Testing";
}
