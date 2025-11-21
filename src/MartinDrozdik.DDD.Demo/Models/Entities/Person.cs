namespace MartinDrozdik.DDD.Demo.Models.Entities;

/// <summary>
/// Represents a legal/actual person entity in the domain.
/// </summary>
public class Person : DomainEntity<PersonId>
{
    public const int FullNameMaxLength = 255;

    /// <summary>
    /// Initializes a new instance of the <see cref="Person"/> class.
    /// </summary>
    /// <param name="id">Identification of a <see cref="Person"/>.</param>
    /// <param name="fullName">The full name of the person.</param>
    /// <param name="dateOfBirth">The date of birth of the person.</param>
    public Person(PersonId id, string fullName, DateTimeOffset dateOfBirth)
        : base(id)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
    }

    /// <summary>
    /// Gets the full name of the person.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the date of birth of the person.
    /// </summary>
    public DateTimeOffset DateOfBirth { get; }

    /// <summary>
    /// Create a new instance of the <see cref="Person"/> class.
    /// </summary>
    /// <param name="fullName">The full name of the person.</param>
    /// <param name="dateOfBirth">The date of birth of the person.</param>
    /// <returns>New instance of <see cref="Person"/> or an <see cref="Error"/>.</returns>
    public static Result<Person, Error> Create(string fullName, DateTimeOffset dateOfBirth)
    {
        return new Person(new PersonId(Guid.CreateVersion7()), fullName, dateOfBirth);
    }

    /// <summary>
    /// State validator for <see cref="Person"/>.
    /// </summary>
    private sealed class Validator : AbstractValidator<(string FullName, DateTimeOffset DateOfBirth)>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).MaximumLength(FullNameMaxLength).NotEmpty();
            RuleFor(x => x.DateOfBirth).LessThan(TimeProvider.System.GetUtcNow().LocalDateTime);
        }

        public static Validator Instance { get; } = new();
    }
}
