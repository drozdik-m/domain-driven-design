using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Enumerations.Errors;
using MartinDrozdik.DDD.Models.Enumerations.Statics;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Enumerations;

/// <summary>
/// A type of dynamic enumeration, where all members are known at runtime and initialized (only once).
/// The enumeration may come with static well-known values for better testability and handling.
/// </summary>
/// <typeparam name="TSelf">Type of the final enumeration class.</typeparam>
public abstract class InitializableEnumeration<TSelf> : Enumeration,
    IEnumerationDeserializer<TSelf>,
    IEnumerationEnumerator<TSelf>
    where TSelf : InitializableEnumeration<TSelf>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializableEnumeration{TSelf}"/> class.
    /// </summary>
    /// <param name="name">Enumeration member name.</param>
    protected InitializableEnumeration(EnumerationName name)
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
    protected internal static IReadOnlyList<TSelf> EnumerationsList { get; set; }
        = Array.Empty<TSelf>();

    /// <summary>
    /// Gets a value indicating whether this enumeration has been initialized.
    /// </summary>
    protected static bool Initialized { get; private set; }

    /// <summary>
    /// Initializes this enumeration with the provided values.
    /// The values must be unique.
    /// </summary>
    /// <param name="values">Values to initialize the enumeration with.</param>
    /// <exception cref="ArgumentException">When duplicate values are found or well-known values are not initialized..</exception>
    public static void Initialize(IEnumerable<TSelf> values)
    {
        values = values.ToArray();

        // Check for duplicates
        values.ThrowIfDuplicateMembers();

        // Cache the enumeration members as a dictionary and a list
        EnumerationsDictionary = values.ToImmutableDictionary(e => e.Name);
        EnumerationsList = values.ToImmutableList();

        // Validate that the static properties are included in the received values
        var staticValues = EnumerationMembers.GetAllStaticMembers<TSelf>().ToArray();
        if (staticValues.Any(e => !EnumerationsDictionary.ContainsKey(e.Name)))
        {
            var invalidStaticValues = staticValues
                .Where(e => !EnumerationsDictionary.ContainsKey(e.Name));

            throw new ArgumentException(
                $"Static values {string.Join(", ", invalidStaticValues.Select(e => e.Name))} are not included in the provided values.");
        }

        // Mark this enumeration as initialized
        Initialized = true;
    }

    /// <summary>
    /// Initializes this enumeration only with the well-known values, defined statically.
    /// </summary>
    /// <remarks>
    /// Well suited for unit tests, where the enumeration is initialized with static values.
    /// </remarks>
    /// <exception cref="ArgumentException">Duplicates are found.</exception>
    public static void InitializeWellKnown()
    {
        var staticValues = EnumerationMembers.GetAllStaticMembers<TSelf>().ToArray();
        Initialize(staticValues);
    }

    /// <inheritdoc cref="IEnumerationDeserializer{TEnumeration}.FromName(EnumerationName)" />
    /// <exception cref="InvalidOperationException">When the enumeration has not been initialized.</exception>
    public static IResult<TSelf, Error> FromName(EnumerationName name)
    {
        ThrowIfNotInitialized();

        // Try to get the enumeration member by name
        if (!EnumerationsDictionary.TryGetValue(name, out var enumeration))
        {
            return Result.Failure<TSelf, Error>(EnumerationErrors.EnumerationNameNotFound<TSelf>(name));
        }

        return Result.Success<TSelf, Error>(enumeration);
    }

    /// <inheritdoc cref="IEnumerationDeserializer{TEnumeration}.FromNameOptional(EnumerationName?)" />
    /// <exception cref="InvalidOperationException">When the enumeration has not been initialized.</exception>
    public static IResult<TSelf?, Error> FromNameOptional(EnumerationName? name)
    {
        ThrowIfNotInitialized();
        return EnumerationMembers.FromNameOptional<TSelf>(name);
    }

    /// <inheritdoc cref="IEnumerationEnumerator{TSelf}.GetAll()" />
    /// <exception cref="InvalidOperationException">When the enumeration has not been initialized.</exception>
    public static IEnumerable<TSelf> GetAll()
    {
        ThrowIfNotInitialized();
        return EnumerationsList;
    }

    /// <summary>
    /// Ensures the enumeration has been initialized.
    /// Otherwise, throws an <see cref="InvalidOperationException"/> exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the enumeration has not been initialized.</exception>"
    private static void ThrowIfNotInitialized()
    {
        if (!Initialized)
        {
            throw new InvalidOperationException(
                $"The enumeration {typeof(TSelf).Name} has not been initialized. Call {nameof(Initialize)} method first.");
        }
    }
}
