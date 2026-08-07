using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Testing.Attributes;
using Xunit;

namespace MartinDrozdik.DDD.Testing.Enumerations;

/// <summary>
/// Base class verifying that an <see cref="Enumeration"/> and the plain .NET <see cref="Enum"/> it is exposed as map cleanly in both directions.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="Enumeration"/> is a domain type and should not leave the domain, so an API contract usually carries a plain .NET enum instead.
/// The two are matched by name by <see cref="EnumerationStructExtensions.ToStructEnum{TEnum}(Enumeration)"/> and <see cref="IStructEnumDeserializer{TEnumeration}.FromStructEnum{TEnum}(TEnum)"/>,
/// which throw when a member has no counterpart — a broken mapping contract that otherwise surfaces only once production reaches that member.
/// </para>
/// <para>
/// Derive once per enumeration that appears on an API contract and every check comes for free.
/// </para>
/// <para>
/// An <see cref="InitializableEnumeration{TSelf}"/> must be initialized before the tests run, because they list its members.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class InvoiceStateMappingTests : EnumerationStructMappingTests&lt;InvoiceState, InvoiceStateDto&gt;;
/// </code>
/// </example>
/// <typeparam name="TEnumeration">Type of the enumeration.</typeparam>
/// <typeparam name="TEnum">Type of the .NET enum.</typeparam>
public abstract class EnumerationStructMappingTests<TEnumeration, TEnum>
    where TEnumeration : Enumeration, IEnumerationEnumerator<TEnumeration>, IStructEnumDeserializer<TEnumeration>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Verifies that both sides have the same members.
    /// </summary>
    [Fact]
    public void Enumeration_and_struct_enum_map_one_to_one()
    {
        // Arrange & Act & Assert
        try
        {
            EnumerationStructMapping.ThrowIfIncomplete<TEnumeration, TEnum>();
        }
        catch (ArgumentException exception)
        {
            Assert.Fail(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            Assert.Fail(exception.Message);
        }
    }

    /// <summary>
    /// Verifies that every enumeration member converts to its .NET enum counterpart.
    /// </summary>
    [Fact]
    public void Every_enumeration_member_converts_to_a_struct_enum_member()
    {
        // Arrange
        var failures = new List<string>();

        // Act
        foreach (var member in TEnumeration.GetAll())
        {
            try
            {
                _ = member.ToStructEnum<TEnum>();
            }
            catch (BusinessRuleException)
            {
                failures.Add(member.Name.Key);
            }
        }

        // Assert
        AssertNoFailures($"member(s) that do not convert to {typeof(TEnum).Name}", failures);
    }

    /// <summary>
    /// Verifies that every .NET enum member converts to its enumeration counterpart.
    /// </summary>
    /// <remarks>
    /// Undefined values arriving off the wire are a separate concern, handled by validation.
    /// </remarks>
    [Fact]
    public void Every_struct_enum_member_converts_to_an_enumeration_member()
    {
        // Arrange
        var failures = new List<string>();

        // Act
        foreach (var value in Enum.GetValues<TEnum>())
        {
            try
            {
                _ = TEnumeration.FromStructEnum(value);
            }
            catch (BusinessRuleException)
            {
                failures.Add(value.ToString());
            }
        }

        // Assert
        AssertNoFailures($"member(s) that do not convert to {typeof(TEnumeration).Name}", failures);
    }

    /// <summary>
    /// Verifies that the domain to API to domain direction is the identity.
    /// </summary>
    [Fact]
    public void Enumeration_members_survive_a_round_trip()
    {
        // Arrange
        var failures = new List<string>();

        // Act
        foreach (var member in TEnumeration.GetAll())
        {
            try
            {
                var structEnum = member.ToStructEnum<TEnum>();
                var roundTripped = TEnumeration.FromStructEnum(structEnum);

                if (!member.Equals(roundTripped))
                {
                    failures.Add($"{member.Name.Key} -> {structEnum} -> {roundTripped.Name.Key}");
                }
            }
            catch (BusinessRuleException exception)
            {
                failures.Add($"{member.Name.Key} ({exception.Message})");
            }
        }

        // Assert
        AssertNoFailures($"member(s) that do not survive a round trip through {typeof(TEnum).Name}", failures);
    }

    /// <summary>
    /// Verifies that the API to domain to API direction is the identity.
    /// </summary>
    [Fact]
    public void Struct_enum_members_survive_a_round_trip()
    {
        // Arrange
        var failures = new List<string>();

        // Act
        foreach (var value in Enum.GetValues<TEnum>())
        {
            try
            {
                var member = TEnumeration.FromStructEnum(value);
                var roundTripped = member.ToStructEnum<TEnum>();

                if (!EqualityComparer<TEnum>.Default.Equals(value, roundTripped))
                {
                    failures.Add($"{value} -> {member.Name.Key} -> {roundTripped}");
                }
            }
            catch (BusinessRuleException exception)
            {
                failures.Add($"{value} ({exception.Message})");
            }
        }

        // Assert
        AssertNoFailures($"member(s) that do not survive a round trip through {typeof(TEnumeration).Name}", failures);
    }

    /// <summary>
    /// Fails the test when any member was collected, naming the mapping and every offending member.
    /// </summary>
    /// <param name="problem">Description of what is wrong with the collected members.</param>
    /// <param name="failures">The offending members.</param>
    [AssertionMethod]
    private static void AssertNoFailures(string problem, IReadOnlyCollection<string> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        Assert.Fail($"{typeof(TEnumeration).Name} -> {typeof(TEnum).Name}: {failures.Count} {problem}: {string.Join(", ", failures)}");
    }
}
