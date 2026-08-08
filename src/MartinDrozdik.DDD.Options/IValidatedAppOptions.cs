using FluentValidation;

namespace MartinDrozdik.DDD.Options;

/// <summary>
/// Represents validated application options.
/// </summary>
/// <typeparam name="TOptions">Concrete type of this options.</typeparam>
public interface IValidatedAppOptions<TOptions> : IAppOptions
    where TOptions : class, IValidatedAppOptions<TOptions>
{
    /// <summary>
    /// Gets the validator that should validate these options.
    /// </summary>
    static abstract AbstractValidator<TOptions> Validator { get; }
}
