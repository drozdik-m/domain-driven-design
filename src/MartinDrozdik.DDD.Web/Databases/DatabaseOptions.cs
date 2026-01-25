using FluentValidation;
using MartinDrozdik.DDD.Web.Options;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// Options for (usually an SQL) database connection.
/// </summary>
public class DatabaseOptions : IValidatedAppOptions<DatabaseOptions>
{
    /// <inheritdoc cref="IAppOptions.Section" />
    public static string Section { get; } = "App:Database";

    /// <inheritdoc cref="IValidatedAppOptions{TOptions}.Validator" />
    public static AbstractValidator<DatabaseOptions> Validator { get; } = new DbConnectionOptionsValidator();

    /// <summary>
    /// Gets the database connection string.
    /// </summary>
    public required string ConnectionString { get; init; }

    private sealed class DbConnectionOptionsValidator : AbstractValidator<DatabaseOptions>
    {
        public DbConnectionOptionsValidator()
        {
            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithMessage("Database connection string must be provided.");
        }
    }
}
