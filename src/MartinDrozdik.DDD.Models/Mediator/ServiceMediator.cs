using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Exceptions;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Mediator;

/// <inheritdoc cref="IMediator" />
public class ServiceMediator(IServiceProvider provider) : IMediator
{
    /// <inheritdoc />
    public Task<TResponse> SendQuery<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IQuery<TResponse>
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<IQueryHandler<TRequest, TResponse>>()
            ?? throw new MediatorException($"No {nameof(IQueryHandler<TRequest, TResponse>)} registered for {typeof(TRequest).Name}");

        // Resolve any pipeline behaviors for the query
        var pipeline = provider.GetService<IPipelineBehavior<TRequest, TResponse>>()
            ?? EmptyPipeline<TRequest, TResponse>.Instance;

        // Handle the query
        return pipeline.HandleQueryAsync(request, handler, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResponse> SendCommand<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand<TResponse>
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<ICommandHandler<TRequest, TResponse>>()
            ?? throw new MediatorException($"No {nameof(ICommandHandler<TRequest, TResponse>)} registered for {typeof(TRequest).Name}");

        // Resolve any pipeline behaviors for the command
        var pipeline = provider.GetService<IPipelineBehavior<TRequest, TResponse>>()
            ?? EmptyPipeline<TRequest, TResponse>.Instance;

        // Handle the command
        return pipeline.HandleCommandAsync(request, handler, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendCommand<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ICommand
    {
        // Resolve the command handler from the service provider
        var handler = provider.GetService<ICommandHandler<TRequest>>()
            ?? throw new MediatorException($"No {nameof(ICommandHandler<TRequest>)} handler registered for {typeof(TRequest).Name}");

        // Resolve any pipeline behaviors for the command
        var pipeline = provider.GetService<IPipelineBehavior<TRequest>>()
            ?? EmptyPipeline<TRequest>.Instance;

        // Handle the command
        return pipeline.HandleCommandAsync(request, handler, cancellationToken);
    }
}
