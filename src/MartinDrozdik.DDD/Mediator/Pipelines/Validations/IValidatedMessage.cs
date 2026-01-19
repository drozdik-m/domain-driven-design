using FluentValidation;

namespace MartinDrozdik.DDD.Mediator.Pipelines.Validations;

/// <summary>
/// Represents a mediator message that requires validation.
/// </summary>
/// <typeparam name="TMessage">The validated message.</typeparam>
public interface IValidatedMessage<TMessage> : IMessage
{
    /// <summary>
    /// Gets the validator that should validate the message.
    /// </summary>
    AbstractValidator<TMessage> Validator { get; }
}
