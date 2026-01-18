namespace MartinDrozdik.DDD.Mediator.Commands;

/// <inheritdoc cref="ICommandHandler{TCommand}"/>
/// <typeparam name="TCommand">The command to be handled.</typeparam>
/// <typeparam name="TResponse">The type of the response expected from the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <inheritdoc cref="ICommandHandler{TCommand}.HandleAsync(TCommand, CancellationToken)" />
    /// <returns>Result of the execution.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
