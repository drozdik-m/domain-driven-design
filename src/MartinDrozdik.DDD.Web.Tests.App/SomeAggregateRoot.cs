using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Web.Tests.App;

/// <summary>
/// Just a random entity for <see cref="TestDbContext"/>.
/// </summary>
public class SomeAggregateRoot : IAggregateRoot<int>
{
    /// <summary>
    /// Gets ID for the random entity.
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// Just a random entity for <see cref="TestDbContext"/>.
/// </summary>
public class SomeDomainEntity : IDomainEntity<int>
{
    /// <summary>
    /// Gets ID for the random entity.
    /// </summary>
    public int Id { get; set; }
}
