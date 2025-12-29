using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Queries;

namespace MartinDrozdik.DDD.Models.Mediator;

/// <summary>
/// Mediator for sending and handling requests.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Asynchronously send a query request to a single handler.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request.</typeparam>
    /// <typeparam name="TResponse">Type of the response.</typeparam>
    /// <param name="request">The request to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response.</returns>
    Task<TResponse> SendQuery<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IQuery<TResponse>;

    /// <summary>
    /// Asynchronously send a command request to a single handler with a reponse.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request.</typeparam>
    /// <typeparam name="TResponse">Type of the response.</typeparam>
    /// <param name="request">The request to be sent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response.</returns>
    Task<TResponse> SendCommand<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand<TResponse>;

    /// <summary>
    /// Asynchronously send a command request to a single handler with no response.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request.</typeparam>
    /// <param name="request">Type of the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the send operation.</returns>
    Task SendCommand<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand;
}
