using MartinDrozdik.DDD.Extensions;
using Microsoft.Extensions.Configuration;

namespace MartinDrozdik.DDD.Web.Options;

/// <summary>
/// Extensions for <see cref="IConfigurationManager"/>.
/// </summary>
public static class ConfigurationManagerExtensions
{
    /// <summary>
    /// Gets the options of type <typeparamref name="TOptions"/> from the configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="configurationManager">The configuration manager to get the options from.</param>
    /// <returns>The requested <typeparamref name="TOptions"/> or null.</returns>
    public static TOptions? GetOptions<TOptions>(this IConfigurationManager configurationManager)
        where TOptions : IAppOptions
    {
        return configurationManager
            .GetSection(TOptions.Section)
            .Get<TOptions>();
    }

    /// <summary>
    /// Gets the options of type <typeparamref name="TOptions"/> from the configuration.
    /// Throws an exception if the options cannot be bound (e.g. section missing, malformed, etc.).
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="configurationManager">The configuration manager to get the options from.</param>
    /// <returns>The requested <typeparamref name="TOptions"/>.</returns>
    public static TOptions GetRequiredOptions<TOptions>(this IConfigurationManager configurationManager)
        where TOptions : IAppOptions
    {
        var options = configurationManager.GetOptions<TOptions>();

        if (options is null)
        {
            throw new InvalidOperationException(
                $"Failed to bind configuration section '{TOptions.Section}' to type '{typeof(TOptions).FullName}'. " +
                "Ensure the section exists and is properly formatted.");
        }

        return options;
    }

    /// <summary>
    /// Gets the options of type <typeparamref name="TOptions"/> from the configuration.
    /// Validates the options if they are not null.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IValidatedAppOptions{TOptions}"/>.</typeparam>
    /// <param name="configurationManager">The configuration manager to get the options from.</param>
    /// <returns>The requested <typeparamref name="TOptions"/> or null.</returns>
    public static TOptions? GetValidatedOptions<TOptions>(this IConfigurationManager configurationManager)
        where TOptions : class, IValidatedAppOptions<TOptions>
    {
        var options = configurationManager
            .GetSection(TOptions.Section)
            .Get<TOptions>();

        if (options is not null)
        {
            var validator = TOptions.Validator;
            validator.ValidateAndThrowBusiness(options);
        }

        return options;
    }

    /// <summary>
    /// Gets the options of type <typeparamref name="TOptions"/> from the configuration.
    /// Validates the options.
    /// Throws an exception if the options cannot be bound (e.g. section missing, malformed, etc.).
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IValidatedAppOptions{TOptions}"/>.</typeparam>
    /// <param name="configurationManager">The configuration manager to get the options from.</param>
    /// <returns>The requested <typeparamref name="TOptions"/>.</returns>
    public static TOptions GetRequiredValidatedOptions<TOptions>(this IConfigurationManager configurationManager)
        where TOptions : class, IValidatedAppOptions<TOptions>
    {
        var requiredOptions = configurationManager.GetValidatedOptions<TOptions>();

        if (requiredOptions is null)
        {
            throw new InvalidOperationException(
                $"Failed to bind configuration section '{TOptions.Section}' to type '{typeof(TOptions).FullName}'. " +
                "Ensure the section exists and is properly formatted.");
        }

        return requiredOptions;
    }
}
