using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Mediator.Queries;

/// <summary>
/// Represents a query handler in the CQRS pattern.
/// Handlers are responsible for processing queries and returning results.
/// </summary>
/// <typeparam name="TQuery">The query to be handled.</typeparam>
/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the specified query and returns a result.
    /// </summary>
    /// <param name="query">The query to be executed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the execution.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
