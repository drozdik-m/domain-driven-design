using MartinDrozdik.DDD.Templates;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// Extensions for <see cref="IMutableModel"/>.
/// </summary>
public static class MutableModelExtensions
{
    /// <summary>
    /// Gets the entity types in the given <see cref="IMutableModel"/> whose CLR types are aggregate roots.
    /// </summary>
    /// <param name="mutableModel">The <see cref="IMutableModel"/> to examine for aggregate-root entity types.</param>
    /// <returns>A sequence of <see cref="IMutableEntityType"/> representing entity types whose CLR types are aggregate roots.</returns>
    public static IEnumerable<IMutableEntityType> GetAggregateRoots(this IMutableModel mutableModel)
    {
        return mutableModel.GetEntityTypes()
            .Where(e => e.ClrType.IsAggregateRoot());
    }

    /// <summary>
    /// Gets all entity types from the specified <see cref="IMutableModel"/> whose CLR types are domain entities.
    /// </summary>
    /// <param name="mutableModel">The <see cref="IMutableModel"/> to enumerate entity types from.</param>
    /// <returns>A sequence of <see cref="IMutableEntityType"/> representing entity types whose CLR types are domain entities.</returns>
    public static IEnumerable<IMutableEntityType> GetDomainEntities(this IMutableModel mutableModel)
    {
        return mutableModel.GetEntityTypes()
            .Where(e => e.ClrType.IsDomainEntity());
    }
}
