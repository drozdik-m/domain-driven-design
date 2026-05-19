namespace MartinDrozdik.DDD.Testing.Attributes;

/// <summary>
/// Marks a method as a custom assertion.
/// </summary>
/// <remarks>
/// For SonarAnalyzer rule S2699.
/// Also consider using JetBrains.Annotations.AssertionMethodAttribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AssertionMethodAttribute : Attribute
{
}
