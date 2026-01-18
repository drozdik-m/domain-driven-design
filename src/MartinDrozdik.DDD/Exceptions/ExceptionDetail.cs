namespace MartinDrozdik.DDD.Exceptions;

/// <summary>
/// Represents a detail of an exception.
/// </summary>
/// <param name="Key">Key of the detail.</param>
/// <param name="Value">Descriptive value of the detail.</param>
public record struct ExceptionDetail(string Key, string Value);
