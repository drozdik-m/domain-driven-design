using System.Diagnostics.CodeAnalysis;
using MartinDrozdik.DDD.Results.Exceptions;
using MartinDrozdik.DDD.Results.Internal;

namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Outcome of an operation that produces a value of type <typeparamref name="T"/> on success and an error of type <typeparamref name="E"/> on failure.
/// </summary>
/// <typeparam name="T">Type of the value returned by a successful operation.</typeparam>
/// <typeparam name="E">Type of the error returned by a failed operation.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
/// <seealso cref="UnitResult{E}"/>
public readonly struct Result<T, E> : IResult<T, E>, IEquatable<Result<T, E>>
{
    // TODO (C# 15 / .NET 11, GA expected November 2026): revisit modelling this as a union type.
    // Declaring `union Result<T, E>` over a success case and a failure case would buy
    // compiler-verified exhaustive matching and retire the hand-written guards below. The catch
    // is storage: a declared union compiles to a struct whose only field is an object reference,
    // so every success would box the value and every failure the error, which is a regression
    // for a type this hot (every enumeration deserialization, every handler response).
    // The upgrade worth taking is the custom union instead: keep the layout below, mark it with
    // the Union attribute from System.Runtime.CompilerServices, and expose one case type per
    // state, so callers get exhaustiveness checking with no boxing. That is blocked until union
    // member providers and the runtime polyfills ship; they were still missing in .NET 11
    // Preview 2/3, where the union attribute and its interface had to be declared by hand.
    // Re-evaluate at .NET 11 GA, and only adopt it if the allocation profile stays flat.
    // Tracked in ROADMAP.md. The same note applies to UnitResult of E.
    private readonly T? _value;
    private readonly E? _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T, E}"/> struct.
    /// </summary>
    /// <param name="isFailure">Whether the operation failed.</param>
    /// <param name="value">The value of a successful operation.</param>
    /// <param name="error">The error of a failed operation.</param>
    /// <exception cref="ArgumentNullException">When a failure is created without an error.</exception>
    internal Result(bool isFailure, T? value, E? error)
    {
        if (isFailure && error is null)
        {
            throw new ArgumentNullException(nameof(error), ResultMessages.ErrorIsNotProvidedForFailure);
        }

        IsFailure = isFailure;
        _value = isFailure ? default : value;
        _error = isFailure ? error : default;
    }

    /// <inheritdoc/>
    public bool IsFailure { get; }

    /// <inheritdoc/>
    public bool IsSuccess => !IsFailure;

    /// <inheritdoc/>
    public T Value => IsFailure
        ? throw new ResultFailureException<E>(_error!)
        : _value!;

    /// <inheritdoc/>
    public E Error => IsFailure
        ? _error!
        : throw new ResultSuccessException();

    /// <summary>
    /// Implicit conversion from a value to a successful result.
    /// </summary>
    /// <param name="value">The value of the successful operation.</param>
    public static implicit operator Result<T, E>(T value)
        => new(false, value, default);

    /// <summary>
    /// Implicit conversion from an error to a failed result.
    /// </summary>
    /// <param name="error">The error of the failed operation.</param>
    public static implicit operator Result<T, E>(E error)
        => new(true, default, error);

    /// <summary>
    /// Implicit conversion to a <see cref="UnitResult{E}"/>, discarding the value.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static implicit operator UnitResult<E>(Result<T, E> result)
        => new(result.IsFailure, result._error);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The left result.</param>
    /// <param name="right">The right result.</param>
    /// <returns>True when both results carry equal values or equal errors.</returns>
    public static bool operator ==(Result<T, E> left, Result<T, E> right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two results are different.
    /// </summary>
    /// <param name="left">The left result.</param>
    /// <param name="right">The right result.</param>
    /// <returns>True when the results are not equal.</returns>
    public static bool operator !=(Result<T, E> left, Result<T, E> right)
        => !left.Equals(right);

    /// <summary>
    /// Gets the value of a successful operation, or the default value of <typeparamref name="T"/> on failure.
    /// </summary>
    /// <returns>The value, or the default value of <typeparamref name="T"/>.</returns>
    public T? GetValueOrDefault()
        => IsFailure ? default : _value;

    /// <summary>
    /// Gets the value of a successful operation, or a fallback on failure.
    /// </summary>
    /// <param name="defaultValue">The value returned when the operation failed.</param>
    /// <returns>The value, or <paramref name="defaultValue"/>.</returns>
    public T GetValueOrDefault(T defaultValue)
        => IsFailure ? defaultValue : _value!;

    /// <summary>
    /// Gets the value without throwing when the operation failed.
    /// </summary>
    /// <param name="value">The value of a successful operation.</param>
    /// <returns>True when the operation succeeded.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsSuccess;
    }

    /// <summary>
    /// Gets the error without throwing when the operation succeeded.
    /// </summary>
    /// <param name="error">The error of a failed operation.</param>
    /// <returns>True when the operation failed.</returns>
    public bool TryGetError([MaybeNullWhen(false)] out E error)
    {
        error = _error;
        return IsFailure;
    }

    /// <summary>
    /// Deconstructs the result into its value and error.
    /// </summary>
    /// <param name="value">The value of a successful operation, otherwise the default value.</param>
    /// <param name="error">The error of a failed operation, otherwise the default value.</param>
    public void Deconstruct(out T? value, out E? error)
    {
        value = _value;
        error = _error;
    }

    /// <inheritdoc/>
    public bool Equals(Result<T, E> other)
    {
        if (IsFailure != other.IsFailure)
        {
            return false;
        }

        return IsFailure
            ? EqualityComparer<E>.Default.Equals(_error, other._error)
            : EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is Result<T, E> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => IsFailure ? HashCode.Combine(true, _error) : HashCode.Combine(false, _value);

    /// <inheritdoc/>
    public override string ToString()
        => IsFailure ? $"Failure({_error})" : $"Success({_value})";
}
