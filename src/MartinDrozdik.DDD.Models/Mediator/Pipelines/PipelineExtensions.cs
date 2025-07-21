using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Queries;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;

/// <summary>
/// Extensions for the pipeline behavior to process requests.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Processes a command using the provided pipeline and handler.
    /// </summary>
    /// <typeparam name="TCommand">The command to handle.</typeparam>
    /// <typeparam name="TResponse">The type of the command response.</typeparam>
    /// <param name="pipeline">The pipeline to execute before handling.</param>
    /// <param name="command">The request command to handle.</param>
    /// <param name="handler">The handler of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the execution.</returns>
    public static Task<Result<TResponse, Error>> HandleCommandAsync<TCommand, TResponse>(
        this IPipelineBehavior<TCommand, TResponse> pipeline,
        TCommand command,
        ICommandHandler<TCommand, TResponse> handler,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        return pipeline.HandleAsync(
            command,
            async (cancellationToken) => await handler.HandleAsync(command, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Processes a query using the provided pipeline and handler.
    /// </summary>
    /// <typeparam name="TQuery">The query to handle.</typeparam>
    /// <typeparam name="TResponse">The type of the query response.</typeparam>
    /// <param name="pipeline">The pipeline to execute before handling.</param>
    /// <param name="query">The request query to handle.</param>
    /// <param name="handler">The handler of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the execution.</returns>
    public static Task<Result<TResponse, Error>> HandleQueryAsync<TQuery, TResponse>(
        this IPipelineBehavior<TQuery, TResponse> pipeline,
        TQuery query,
        IQueryHandler<TQuery, TResponse> handler,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        return pipeline.HandleAsync(
            query,
            async (cancellationToken) => await handler.HandleAsync(query, cancellationToken),
            cancellationToken);
    }
}
