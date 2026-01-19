using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Mediator.Pipelines.Validations;

/// <summary>
/// Pipeline that validates requests implementing <see cref="IValidatedMessage{TMessage}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
public class ValidationPipeline<TRequest>() : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    /// <inheritdoc />
    public Task HandleAsync(TRequest input, PipelineNextDelegate next, CancellationToken cancellationToken)
    {
        ThrowIfInvalid(input);
        return next(cancellationToken);
    }

    /// <summary>
    /// Throws <see cref="BusinessRuleValidationException"/> if the request is invalid.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    protected static void ThrowIfInvalid(TRequest request)
    {
        if (request is IValidatedMessage<TRequest> validatableRequest)
        {
            var validator = validatableRequest.Validator;
            var validationResult = validator.Validate(request);
            if (validationResult.TryGetException(out var exception))
            {
                throw exception;
            }
        }
    }
}
