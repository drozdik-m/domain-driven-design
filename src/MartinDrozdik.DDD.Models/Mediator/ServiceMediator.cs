using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Exceptions;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Mediator;

/// <inheritdoc cref="IMediator" />
public class ServiceMediator(IServiceProvider provider) : IMediator
{
    /*/// <inheritdoc />
    public Task<Result<TResult, Error>> SendCommandAsync<TCommand, TResult>(TResult command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
    {

        var handler = provider.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for {typeof(TCommand).Name}");
        }

        return handler.HandleAsync((TCommand)command, cancellationToken);

        var behaviors = provider.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse();

        Func<Task<TResult>> handlerDelegate = () => handler.HandleAsync(command, cancellationToken);
        foreach (var behavior in behaviors)
        {
            var next = handlerDelegate;
            handlerDelegate = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return await handlerDelegate();
    }*/

    /// <inheritdoc />
    public async Task<Result<TResponse, Error>> SendQuery<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IQuery<TResponse>
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<IQueryHandler<TRequest, TResponse>>()
            ?? throw new MediatorException($"No {nameof(IQueryHandler<TRequest, TResponse>)} registered for {typeof(TRequest).Name}");

        // Handle the query using the resolved handler
        var result = await handler.HandleAsync(request, cancellationToken);

        // Return the result
        return result;
    }

    /// <inheritdoc />
    public async Task<Result<TResponse, Error>> SendCommand<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand<TResponse>
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<ICommandHandler<TRequest, TResponse>>()
            ?? throw new MediatorException($"No {nameof(ICommandHandler<TRequest, TResponse>)} registered for {typeof(TRequest).Name}");

        // Handle the command using the resolved handler
        var result = await handler.HandleAsync(request, cancellationToken);

        // Return the result
        return result;
    }

    /// <inheritdoc />
    public async Task<UnitResult<Error>> SendCommand<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<ICommandHandler<TRequest>>()
            ?? throw new MediatorException($"No {nameof(ICommandHandler<TRequest>)} handler registered for {typeof(TRequest).Name}");

        // Handle the command using the resolved handler
        var result = await handler.HandleAsync(request, cancellationToken);

        // Return the result
        return result;
    }
}
