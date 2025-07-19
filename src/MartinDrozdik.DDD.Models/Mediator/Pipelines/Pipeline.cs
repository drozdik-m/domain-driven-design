using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Represents a pipeline that processes input through a series of behaviors.
/// </summary>
/// <typeparam name="TInput">The type of input item.</typeparam>
/// <typeparam name="TOutput">The type of output item.</typeparam>
/// <param name="behaviors">Series of behaviours processed one by one.</param>
public class Pipeline<TInput, TOutput>(IEnumerable<IPipelineBehavior<TInput, TOutput>> behaviors) : IPipelineBehavior<TInput, TOutput>
{
    /// <inheritdoc />
    public async Task<Result<TOutput, Error>> HandleAsync(TInput input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken)
    {
        foreach (var behavior in behaviors)
        {
            next = (cancellationToken) =>
            {
                // Check for cancellation before invoking the behavior
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<Result<TOutput, Error>>(cancellationToken);
                }

                // Invoke the behavior and pass the next delegate
                return behavior.HandleAsync(input, next, cancellationToken);
            };
        }

        return await next(cancellationToken);
    }
}
