namespace MartinDrozdik.DDD.Demo.Models.Entities;

/// <summary>
/// Identity of <see cref="Person"/> entity.
/// </summary>
public class PersonId(Guid key) : GuidIdentity<PersonId>(key);

