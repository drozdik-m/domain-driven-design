namespace MartinDrozdik.DDD.Mediator.Queries;

/// <summary>
/// Represents a query in the CQRS pattern.
/// Queries are used to retrieve data from the system without changing its state.
/// </summary>
/// <typeparam name="TResponse">The type of the response expected from the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
