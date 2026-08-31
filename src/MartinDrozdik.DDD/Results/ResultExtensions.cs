namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Composition helpers for <see cref="Result{T, E}"/> and <see cref="UnitResult{E}"/>.
/// </summary>
/// <remarks>
/// Every method short-circuits on failure.
/// Sourced from <see href="https://github.com/vkhorikov/CSharpFunctionalExtensions"/>.
/// </remarks>
public static class ResultExtensions
{
    /// <summary>
    /// Projects the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the projected value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="selector">Projection of the value.</param>
    /// <returns>The projected result, or the original failure.</returns>
    public static Result<TNew, E> Map<T, TNew, E>(this Result<T, E> result, Func<T, TNew> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.IsFailure
            ? Result.Failure<TNew, E>(result.Error)
            : Result.Success<TNew, E>(selector(result.Value));
    }

    /// <summary>
    /// Projects the error of a failed result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the original error.</typeparam>
    /// <typeparam name="ENew">Type of the projected error.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="selector">Projection of the error.</param>
    /// <returns>The result with a projected error, or the original success.</returns>
    public static Result<T, ENew> MapError<T, E, ENew>(this Result<T, E> result, Func<E, ENew> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.IsFailure
            ? Result.Failure<T, ENew>(selector(result.Error))
            : Result.Success<T, ENew>(result.Value);
    }

    /// <summary>
    /// Projects the error of a failed result.
    /// </summary>
    /// <typeparam name="E">Type of the original error.</typeparam>
    /// <typeparam name="ENew">Type of the projected error.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="selector">Projection of the error.</param>
    /// <returns>The result with a projected error, or the original success.</returns>
    public static UnitResult<ENew> MapError<E, ENew>(this UnitResult<E> result, Func<E, ENew> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.IsFailure
            ? UnitResult.Failure(selector(result.Error))
            : UnitResult.Success<ENew>();
    }

    /// <summary>
    /// Chains another result-producing operation onto a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the resulting value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static Result<TNew, E> Bind<T, TNew, E>(this Result<T, E> result, Func<T, Result<TNew, E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? Result.Failure<TNew, E>(result.Error)
            : binder(result.Value);
    }

    /// <summary>
    /// Chains a value-less operation onto a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static UnitResult<E> Bind<T, E>(this Result<T, E> result, Func<T, UnitResult<E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? UnitResult.Failure(result.Error)
            : binder(result.Value);
    }

    /// <summary>
    /// Chains a value-producing operation onto a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the resulting value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static Result<T, E> Bind<T, E>(this UnitResult<E> result, Func<Result<T, E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? Result.Failure<T, E>(result.Error)
            : binder();
    }

    /// <summary>
    /// Chains another value-less operation onto a successful result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static UnitResult<E> Bind<E>(this UnitResult<E> result, Func<UnitResult<E>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? result
            : binder();
    }

    /// <summary>
    /// Fails a successful result when the value does not satisfy a predicate.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The condition the value has to satisfy.</param>
    /// <param name="error">The error used when the predicate is not satisfied.</param>
    /// <returns>The original result, or a failure carrying <paramref name="error"/>.</returns>
    public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, bool> predicate, E error)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (result.IsFailure || predicate(result.Value))
        {
            return result;
        }

        return Result.Failure<T, E>(error);
    }

    /// <summary>
    /// Fails a successful result when the value does not satisfy a predicate.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The condition the value has to satisfy.</param>
    /// <param name="errorFactory">Builds the error when the predicate is not satisfied.</param>
    /// <returns>The original result, or a failure carrying the built error.</returns>
    public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, bool> predicate, Func<T, E> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (result.IsFailure || predicate(result.Value))
        {
            return result;
        }

        return Result.Failure<T, E>(errorFactory(result.Value));
    }

    /// <summary>
    /// Runs a side effect on the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The side effect to run on success.</param>
    /// <returns>The original result.</returns>
    public static Result<T, E> Tap<T, E>(this Result<T, E> result, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Runs a side effect on a successful result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The side effect to run on success.</param>
    /// <returns>The original result.</returns>
    public static UnitResult<E> Tap<E>(this UnitResult<E> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Runs a side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static Result<T, E> TapError<T, E>(this Result<T, E> result, Action<E> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Runs a side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static UnitResult<E> TapError<E>(this UnitResult<E> result, Action<E> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="result">The result to collapse.</param>
    /// <param name="onSuccess">Produces the output from the value.</param>
    /// <param name="onFailure">Produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static TOut Match<T, E, TOut>(this Result<T, E> result, Func<T, TOut> onSuccess, Func<E, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsFailure
            ? onFailure(result.Error)
            : onSuccess(result.Value);
    }

    /// <summary>
    /// Runs the branch matching the state of the result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Runs with the value on success.</param>
    /// <param name="onFailure">Runs with the error on failure.</param>
    public static void Match<T, E>(this Result<T, E> result, Action<T> onSuccess, Action<E> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (result.IsFailure)
        {
            onFailure(result.Error);
        }
        else
        {
            onSuccess(result.Value);
        }
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="result">The result to collapse.</param>
    /// <param name="onSuccess">Produces the output on success.</param>
    /// <param name="onFailure">Produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static TOut Match<E, TOut>(this UnitResult<E> result, Func<TOut> onSuccess, Func<E, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsFailure
            ? onFailure(result.Error)
            : onSuccess();
    }

    /// <summary>
    /// Runs the branch matching the state of the result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Runs on success.</param>
    /// <param name="onFailure">Runs with the error on failure.</param>
    public static void Match<E>(this UnitResult<E> result, Action onSuccess, Action<E> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (result.IsFailure)
        {
            onFailure(result.Error);
        }
        else
        {
            onSuccess();
        }
    }
}
