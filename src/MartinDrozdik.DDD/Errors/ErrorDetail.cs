namespace MartinDrozdik.DDD.Errors;

/// <summary>
/// Represents a detail of the error.
/// </summary>
/// <param name="Key">Key of the detail.</param>
/// <param name="Value">Descriptive value of the detail.</param>
public record struct ErrorDetail(string Key, string Value);
