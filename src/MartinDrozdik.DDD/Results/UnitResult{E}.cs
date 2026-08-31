using System.Diagnostics.CodeAnalysis;
using MartinDrozdik.DDD.Results.Exceptions;
using MartinDrozdik.DDD.Results.Internal;

namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Outcome of an operation that produces no value on success and an error of type <typeparamref name="E"/> on failure.
/// </summary>
/// <typeparam name="E">Type of the error returned by a failed operation.</typeparam>
/// <remarks>
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
/// <seealso cref="Result{T, E}"/>
public readonly struct UnitResult<E> : IUnitResult<E>, IEquatable<UnitResult<E>>
{
    // TODO (C# 15 / .NET 11): the union-type note in Result{T,E}.cs applies here too
    private readonly E? _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitResult{E}"/> struct.
    /// </summary>
    /// <param name="isFailure">Whether the operation failed.</param>
    /// <param name="error">The error of a failed operation.</param>
    /// <exception cref="ArgumentNullException">When a failure is created without an error.</exception>
    internal UnitResult(bool isFailure, E? error)
    {
        if (isFailure && error is null)
        {
            throw new ArgumentNullException(nameof(error), ResultMessages.ErrorIsNotProvidedForFailure);
        }

        IsFailure = isFailure;
        _error = isFailure ? error : default;
    }

    /// <inheritdoc/>
    public bool IsFailure { get; }

    /// <inheritdoc/>
    public bool IsSuccess => !IsFailure;

    /// <inheritdoc/>
    public E Error => IsFailure
        ? _error!
        : throw new ResultSuccessException();

    /// <summary>
    /// Implicit conversion from an error to a failed result.
    /// </summary>
    /// <param name="error">The error of the failed operation.</param>
    public static implicit operator UnitResult<E>(E error)
        => new(true, error);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The left result.</param>
    /// <param name="right">The right result.</param>
    /// <returns>True when both results are successes or carry equal errors.</returns>
    public static bool operator ==(UnitResult<E> left, UnitResult<E> right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two results are different.
    /// </summary>
    /// <param name="left">The left result.</param>
    /// <param name="right">The right result.</param>
    /// <returns>True when the results are not equal.</returns>
    public static bool operator !=(UnitResult<E> left, UnitResult<E> right)
        => !left.Equals(right);

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
    /// Deconstructs the result into its state and error.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error of a failed operation, otherwise the default value.</param>
    public void Deconstruct(out bool isSuccess, out E? error)
    {
        isSuccess = IsSuccess;
        error = _error;
    }

    /// <inheritdoc/>
    public bool Equals(UnitResult<E> other)
    {
        if (IsFailure != other.IsFailure)
        {
            return false;
        }

        return IsSuccess || EqualityComparer<E>.Default.Equals(_error, other._error);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is UnitResult<E> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => IsFailure ? HashCode.Combine(true, _error) : HashCode.Combine(false);

    /// <inheritdoc/>
    public override string ToString()
        => IsFailure ? $"Failure({_error})" : "Success";
}
