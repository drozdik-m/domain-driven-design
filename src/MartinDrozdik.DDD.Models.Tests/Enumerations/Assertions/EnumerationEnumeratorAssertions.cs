using MartinDrozdik.DDD.Models.Enumerations;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace MartinDrozdik.DDD.Models.Tests.Enumerations.Assertions;

/// <summary>
/// Assertions for <see cref="IEnumerationEnumerator{TEnumeration}"/> implementations.
/// </summary>
public static class EnumerationEnumeratorAssertions
{
    /// <summary>
    /// Asserts that the <see cref="IEnumerationEnumerator{TEnumeration}.GetAll"/> implementation works as expected.
    /// </summary>
    /// <typeparam name="TEnumeration">The enumeration type implementing <see cref="IEnumerationEnumerator{TEnumeration}"/>.</typeparam>
    /// <param name="expectedMembers">The expected enumeration members.</param>
    public static void AssertGetAll<TEnumeration>(IEnumerable<TEnumeration> expectedMembers)
        where TEnumeration : Enumeration, IEnumerationEnumerator<TEnumeration>
    {
        // Act
        var result = TEnumeration.GetAll().ToList();

        // Assert
        Assert.Equal(expectedMembers.Count(), result.Count);
        Assert.Equivalent(expectedMembers, result);
    }
}
