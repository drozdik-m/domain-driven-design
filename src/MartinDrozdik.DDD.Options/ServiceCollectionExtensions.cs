using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Options;

/// <summary>
/// Extensions for adding new <see cref="IAppOptions"/>s.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IAppOptions"/>.
    /// Binds properly to provided <typeparamref name="TOptions"/> and ensures validation.
    /// </summary>
    /// <remarks>
    /// Validation is registered with <c>ValidateOnStart</c>, which the generic host runs for you.
    /// Code that builds a bare <see cref="IServiceProvider"/> without a host must resolve
    /// <see cref="IStartupValidator"/> and call <see cref="IStartupValidator.Validate"/> itself.
    /// </remarks>
    /// <typeparam name="TOptions">The type of registered options.</typeparam>
    /// <param name="services">Where the options are registered.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppOptions<TOptions>(this IServiceCollection services)
        where TOptions : class, IAppOptions
    {
        var section = TOptions.Section;
        services.AddOptions<TOptions>()
            .BindConfiguration(section, e => e.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IAppOptions"/>.
    /// Binds properly to provided <typeparamref name="TOptions"/> and ensures validation via Fluent Validation.
    /// </summary>
    /// <remarks>
    /// Validation is registered with <c>ValidateOnStart</c>, which the generic host runs for you.
    /// Code that builds a bare <see cref="IServiceProvider"/> without a host must resolve
    /// <see cref="IStartupValidator"/> and call <see cref="IStartupValidator.Validate"/> itself.
    /// </remarks>
    /// <typeparam name="TOptions">The type of registered options.</typeparam>
    /// <param name="services">Where the options are registered.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddValidatedAppOptions<TOptions>(this IServiceCollection services)
        where TOptions : class, IValidatedAppOptions<TOptions>
    {
        // Register options validator
        var fluentValidateOptions = new FluentValidateOptions<TOptions>();
        services.AddSingleton<IValidateOptions<TOptions>>(fluentValidateOptions);

        // Register options with validation
        return services.AddAppOptions<TOptions>();
    }
}
