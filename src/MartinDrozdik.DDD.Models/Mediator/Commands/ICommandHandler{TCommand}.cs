namespace MartinDrozdik.DDD.Models.Mediator.Commands;

/// <summary>
/// Represents a command handler in the CQRS pattern.
/// Handlers are responsible for processing commands and returning results.
/// </summary>
/// <typeparam name="TCommand">The command to be handled.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes the specified command and returns a result.
    /// </summary>
    /// <param name="command">The command to be executed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}
