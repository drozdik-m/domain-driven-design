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
    public async Task<TOutput> HandleAsync(TInput input, PipelineNextDelegate<TOutput> next, CancellationToken cancellationToken)
    {
        // Compose the pipeline by wrapping each behavior around the next delegate
        var composed = next;

        // Iterate through the behaviors to compose them
        foreach (var behavior in behaviors)
        {
            var nextCopy = composed;
            composed = (ct) =>
            {
                // Check if the cancellation token has been requested
                if (ct.IsCancellationRequested)
                {
                    return Task.FromCanceled<TOutput>(ct);
                }

                // Call the current behavior with the input and the next delegate
                return behavior.HandleAsync(input, nextCopy, ct);
            };
        }

        return await composed(cancellationToken);
    }
}
