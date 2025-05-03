using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Enumerations.Errors;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations;

/// <summary>
/// Static enumeration, where all members are known at compile time as static properties.
/// </summary>
/// <typeparam name="TSelf">Type of the final enumeration class.</typeparam>
public abstract class StaticEnumeration<TSelf> : Enumeration,
    IEnumerationDeserializer<TSelf>,
    IEnumerationEnumerator<TSelf>
    where TSelf : StaticEnumeration<TSelf>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaticEnumeration{TSelf}"/> class.
    /// </summary>
    /// <param name="name">Enumeration member name.</param>
    protected StaticEnumeration(EnumerationName name)
        : base(name)
    {
    }

    /// <summary>
    /// Gets or sets dictionary of all enumeration members of this static enumeration.
    /// </summary>
    protected internal static IReadOnlyDictionary<EnumerationName, TSelf> EnumerationsDictionary { get; set; }
        = new Dictionary<EnumerationName, TSelf>();

    /// <summary>
    /// Gets or sets list of all enumeration members of this static enumeration.
    /// </summary>
    protected internal static IReadOnlyList<TSelf> EnumerationsList { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether this enumeration has been initialized.
    /// </summary>
    protected static bool Initialized { get; private set; }

    /// <inheritdoc cref="IEnumerationDeserializer{TEnumeration}.FromName(EnumerationName)" />
    /// <exception cref="ArgumentException">When duplicate values are found.</exception>
    public static IResult<TSelf, Error> FromName(EnumerationName name)
    {
        EnsureInitialized();

        // Try to get the enumeration member by name
        if (!EnumerationsDictionary.TryGetValue(name, out var enumeration))
        {
            return Result.Failure<TSelf, Error>(EnumerationErrors.EnumerationNameNotFound<TSelf>(name));
        }

        return Result.Success<TSelf, Error>(enumeration);
    }

    /// <inheritdoc cref="IEnumerationDeserializer{TEnumeration}.FromNameOptional(EnumerationName?)" />
    /// <exception cref="ArgumentException">When duplicate values are found.</exception>
    public static IResult<TSelf?, Error> FromNameOptional(EnumerationName? name)
    {
        EnsureInitialized();
        return EnumerationMembers.FromNameOptional<TSelf>(name);
    }

    /// <inheritdoc cref="IEnumerationEnumerator{TEnumeration}.GetAll()" />
    /// <exception cref="ArgumentException">When duplicate values are found.</exception>
    public static IEnumerable<TSelf> GetAll()
    {
        EnsureInitialized();
        return EnumerationsList;
    }

    /// <summary>
    /// Initializes this enumeration by filling the cache properties.
    /// </summary>
    /// <exception cref="ArgumentException">When duplicate values are found.</exception>
    protected static void Initialize()
    {
        // Get all static members of this enumeration
        var staticEnumerations = EnumerationMembers
            .GetAllStaticMembers<TSelf>()
            .ToImmutableList();

        // Check for duplicates
        staticEnumerations.ThrowIfDuplicateMembers();

        // Cache the enumeration members as a dictionary and a list
        EnumerationsDictionary = staticEnumerations.ToImmutableDictionary(e => e.Name);
        EnumerationsList = staticEnumerations;

        // Mark as initialized
        Initialized = true;
    }

    /// <summary>
    /// Makes sure the enumeration is initialized.
    /// </summary>
    protected static void EnsureInitialized()
    {
        if (!Initialized)
        {
            Initialize();
        }
    }
}
