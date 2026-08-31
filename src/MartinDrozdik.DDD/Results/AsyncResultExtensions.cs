namespace MartinDrozdik.DDD.Results;

/// <summary>
/// Asynchronous composition helpers for <see cref="Result{T, E}"/> and <see cref="UnitResult{E}"/>.
/// </summary>
/// <remarks>
/// Sourced from https://github.com/vkhorikov/CSharpFunctionalExtensions.
/// </remarks>
public static class AsyncResultExtensions
{
    /// <summary>
    /// Projects the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the projected value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to project.</param>
    /// <param name="selector">Asynchronous projection of the value.</param>
    /// <returns>The projected result, or the original failure.</returns>
    public static async Task<Result<TNew, E>> MapAsync<T, TNew, E>(
        this Result<T, E> result,
        Func<T, Task<TNew>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return result.IsFailure
            ? Result.Failure<TNew, E>(result.Error)
            : Result.Success<TNew, E>(await selector(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Projects the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the projected value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to project.</param>
    /// <param name="selector">Asynchronous projection of the value.</param>
    /// <returns>The projected result, or the original failure.</returns>
    public static async Task<Result<TNew, E>> MapAsync<T, TNew, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task<TNew>> selector)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.MapAsync(selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Chains another result-producing operation onto a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the resulting value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The asynchronous operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static async Task<Result<TNew, E>> BindAsync<T, TNew, E>(
        this Result<T, E> result,
        Func<T, Task<Result<TNew, E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? Result.Failure<TNew, E>(result.Error)
            : await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Chains another result-producing operation onto a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the original value.</typeparam>
    /// <typeparam name="TNew">Type of the resulting value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to chain onto.</param>
    /// <param name="binder">The asynchronous operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static async Task<Result<TNew, E>> BindAsync<T, TNew, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task<Result<TNew, E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    /// Chains another value-less operation onto a successful result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to chain onto.</param>
    /// <param name="binder">The asynchronous operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static async Task<UnitResult<E>> BindAsync<E>(
        this UnitResult<E> result,
        Func<Task<UnitResult<E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return result.IsFailure
            ? result
            : await binder().ConfigureAwait(false);
    }

    /// <summary>
    /// Chains another value-less operation onto a successful result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to chain onto.</param>
    /// <param name="binder">The asynchronous operation to run on success.</param>
    /// <returns>The result of <paramref name="binder"/>, or the original failure.</returns>
    public static async Task<UnitResult<E>> BindAsync<E>(
        this Task<UnitResult<E>> resultTask,
        Func<Task<UnitResult<E>>> binder)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an asynchronous side effect on the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on success.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T, E>> TapAsync<T, E>(
        this Result<T, E> result,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Runs an asynchronous side effect on the value of a successful result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on success.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T, E>> TapAsync<T, E>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an asynchronous side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T, E>> TapErrorAsync<T, E>(
        this Result<T, E> result,
        Func<E, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Runs an asynchronous side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T, E>> TapErrorAsync<T, E>(
        this Task<Result<T, E>> resultTask,
        Func<E, Task> action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an asynchronous side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="result">The result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static async Task<UnitResult<E>> TapErrorAsync<E>(
        this UnitResult<E> result,
        Func<E, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Runs an asynchronous side effect on the error of a failed result.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <param name="resultTask">The task producing the result to observe.</param>
    /// <param name="action">The asynchronous side effect to run on failure.</param>
    /// <returns>The original result.</returns>
    public static async Task<UnitResult<E>> TapErrorAsync<E>(
        this Task<UnitResult<E>> resultTask,
        Func<E, Task> action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="result">The result to collapse.</param>
    /// <param name="onSuccess">Asynchronously produces the output from the value.</param>
    /// <param name="onFailure">Asynchronously produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static async Task<TOut> MatchAsync<T, E, TOut>(
        this Result<T, E> result,
        Func<T, Task<TOut>> onSuccess,
        Func<E, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsFailure
            ? await onFailure(result.Error).ConfigureAwait(false)
            : await onSuccess(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="resultTask">The task producing the result to collapse.</param>
    /// <param name="onSuccess">Asynchronously produces the output from the value.</param>
    /// <param name="onFailure">Asynchronously produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static async Task<TOut> MatchAsync<T, E, TOut>(
        this Task<Result<T, E>> resultTask,
        Func<T, Task<TOut>> onSuccess,
        Func<E, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="result">The result to collapse.</param>
    /// <param name="onSuccess">Asynchronously produces the output on success.</param>
    /// <param name="onFailure">Asynchronously produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static async Task<TOut> MatchAsync<E, TOut>(
        this UnitResult<E> result,
        Func<Task<TOut>> onSuccess,
        Func<E, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsFailure
            ? await onFailure(result.Error).ConfigureAwait(false)
            : await onSuccess().ConfigureAwait(false);
    }

    /// <summary>
    /// Collapses a result into a single value.
    /// </summary>
    /// <typeparam name="E">Type of the error.</typeparam>
    /// <typeparam name="TOut">Type of the produced value.</typeparam>
    /// <param name="resultTask">The task producing the result to collapse.</param>
    /// <param name="onSuccess">Asynchronously produces the output on success.</param>
    /// <param name="onFailure">Asynchronously produces the output from the error.</param>
    /// <returns>The output of the matching branch.</returns>
    public static async Task<TOut> MatchAsync<E, TOut>(
        this Task<UnitResult<E>> resultTask,
        Func<Task<TOut>> onSuccess,
        Func<E, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }
}
