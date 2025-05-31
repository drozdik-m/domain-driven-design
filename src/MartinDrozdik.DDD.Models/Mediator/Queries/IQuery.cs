namespace MartinDrozdik.DDD.Models.Mediator.Queries;

#pragma warning disable S2326 // Unused type parameters should be removed
/// <summary>
/// Represents a query in the CQRS pattern.
/// Queries are used to retrieve data from the system without changing its state.
/// </summary>
/// <typeparam name="TResponse">The type of the response expected from the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
#pragma warning restore S2326 // Unused type parameters should be removed
