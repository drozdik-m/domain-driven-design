namespace MartinDrozdik.DDD.Mediator;

#pragma warning disable S2326 // Unused type parameters should be removed
/// <summary>
/// A mediator request with a response.
/// </summary>
/// <typeparam name="TResponse">Type of the requests' response.</typeparam>
public interface IRequest<out TResponse> : IRequest
{
}
#pragma warning restore S2326 // Unused type parameters should be removed
