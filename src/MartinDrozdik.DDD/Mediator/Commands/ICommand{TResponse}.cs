namespace MartinDrozdik.DDD.Models.Mediator.Commands;

/// <inheritdoc cref="ICommand"/>
/// <typeparam name="TResponse">The type of the response expected from the command.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
