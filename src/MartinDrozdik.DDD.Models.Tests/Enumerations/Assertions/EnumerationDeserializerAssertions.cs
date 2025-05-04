using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Enumerations;
using Xunit;
using MartinDrozdik.DDD.Models.Enumerations.Errors;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Assertions;

/// <summary>
/// Assertions for <see cref="IEnumerationDeserializer{TEnumeration}"/> implementations.
/// </summary>
public static class EnumerationDeserializerAssertions
{
    /// <summary>
    /// Asserts that both <see cref="IEnumerationDeserializer{TEnumeration}.FromName"/> and <see cref="IEnumerationDeserializer{TEnumeration}.FromNameOptional"/> implementations work as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IEnumerationDeserializer{TEnumeration}"/>.</typeparam>
    /// <param name="validName">A valid <see cref="EnumerationName"/> that should deserialize successfully.</param>
    /// <param name="invalidName">An invalid <see cref="EnumerationName"/> that should fail deserialization.</param>
    /// <param name="expectedValue">The expected enumeration value for the valid name.</param>
    public static void AssertEnumerationDeserializer<TEnumeration>(
        EnumerationName validName,
        EnumerationName invalidName,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        AssertFromName(validName, invalidName, expectedValue);
        AssertFromNameOptional(validName, invalidName, expectedValue);
    }

    /// <summary>
    /// Asserts that the <see cref="IEnumerationDeserializer{TEnumeration}.FromName"/> implementation works as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IEnumerationDeserializer{TEnumeration}"/>.</typeparam>
    /// <param name="validName">A valid <see cref="EnumerationName"/> that should deserialize successfully.</param>
    /// <param name="invalidName">An invalid <see cref="EnumerationName"/> that should fail deserialization.</param>
    /// <param name="expectedValue">The expected enumeration value for the valid name.</param>
    public static void AssertFromName<TEnumeration>(
        EnumerationName validName,
        EnumerationName invalidName,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        // Valid name
        var validResult = TEnumeration.FromName(validName);
        Assert.True(validResult.IsSuccess, $"{nameof(IEnumerationDeserializer<TEnumeration>.FromName)} failed for valid name '{validName}'");
        Assert.Equal(expectedValue, validResult.Value);

        // Invalid name
        var invalidResult = TEnumeration.FromName(invalidName);
        Assert.True(invalidResult.IsFailure, $"{nameof(IEnumerationDeserializer<TEnumeration>.FromName)} succeeded for invalid name '{invalidName}', but it should have failed.");
        Assert.Equal(EnumerationErrorCodes.EnumerationNameNotFound, invalidResult.Error?.Code);
        Assert.NotNull(invalidResult.Error);
    }

    /// <summary>
    /// Asserts that the <see cref="IEnumerationDeserializer{TEnumeration}.FromNameOptional"/> implementation works as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IEnumerationDeserializer{TEnumeration}"/>.</typeparam>
    /// <param name="validName">A valid <see cref="EnumerationName"/> that should deserialize successfully.</param>
    /// <param name="invalidName">An invalid <see cref="EnumerationName"/> that should fail deserialization.</param>
    /// <param name="expectedValue">The expected enumeration value for the valid name.</param>
    public static void AssertFromNameOptional<TEnumeration>(
        EnumerationName validName,
        EnumerationName invalidName,
        TEnumeration expectedValue)
        where TEnumeration : Enumeration, IEnumerationDeserializer<TEnumeration>
    {
        // Valid name
        var validResult = TEnumeration.FromNameOptional(validName);
        Assert.True(validResult.IsSuccess, $"{nameof(IEnumerationDeserializer<TEnumeration>.FromNameOptional)} failed for valid name '{validName}'");
        Assert.Equal(expectedValue, validResult.Value);

        // Invalid name
        var invalidResult = TEnumeration.FromNameOptional(invalidName);
        Assert.True(invalidResult.IsFailure, $"{nameof(IEnumerationDeserializer<TEnumeration>.FromNameOptional)} succeeded for invalid name '{invalidName}', but it should have failed.");
        Assert.Equal(EnumerationErrorCodes.EnumerationNameNotFound, invalidResult.Error?.Code);
        Assert.NotNull(invalidResult.Error);

        // Null name
        var nullResult = TEnumeration.FromNameOptional(null);
        Assert.True(nullResult.IsSuccess, $"{nameof(IEnumerationDeserializer<TEnumeration>.FromNameOptional)} failed for null name.");
        Assert.Null(nullResult.Value);
    }
}
