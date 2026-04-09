using FluentValidation;
using MartinDrozdik.DDD.Web.Options;

namespace MartinDrozdik.DDD.Web.FilePathProviders.StaticResources;

/// <summary>
/// Options for static file versioning.
/// </summary>
public class StaticFileVersioningOptions : IValidatedAppOptions<StaticFileVersioningOptions>
{
    /// <inheritdoc cref="IAppOptions.Section" />
    public static string Section { get; } = "App:StaticFileVersioning";

    /// <inheritdoc cref="IValidatedAppOptions{TOptions}.Validator" />
    public static AbstractValidator<StaticFileVersioningOptions> Validator { get; } = new StaticFileVersioningOptionsValidator();

    /// <summary>
    /// Gets the version of static files.
    /// </summary>
    public required Version Version { get; init; }

    private sealed class StaticFileVersioningOptionsValidator : AbstractValidator<StaticFileVersioningOptions>
    {
        public StaticFileVersioningOptionsValidator()
        {
            RuleFor(x => x.Version)
                .NotEmpty()
                .WithMessage("Static file version must not be empty.");
        }
    }
}
