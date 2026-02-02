using System.Linq.Expressions;
using Microsoft.AspNetCore.Hosting;

namespace MartinDrozdik.DDD.Web.Options;

/// <summary>
/// Extensions for <see cref="IWebHostBuilder"/>.
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Sets a configuration setting with strong type safety using IAppOptions.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="builder">The builder to modify.</param>
    /// <param name="propertySelector">Expression to select the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The <see cref="IWebHostBuilder"/> for chaining.</returns>
    public static IWebHostBuilder SetOption<TOptions>(
        this IWebHostBuilder builder,
        Expression<Func<TOptions, object>> propertySelector,
        string value)
        where TOptions : IAppOptions
    {
        var propertyName = GetPropertyName(propertySelector);
        var section = TOptions.Section;
        var fullKey = $"{section}:{propertyName}";

        return builder.UseSetting(fullKey, value);
    }

    /// <summary>
    /// Gets the property name from the expression.
    /// </summary>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="propertySelector">Expression to select the property.</param>
    /// <returns>Name of the selected property.</returns>
    private static string GetPropertyName<TOptions>(Expression<Func<TOptions, object>> propertySelector)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (propertySelector.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operand)
        {
            return operand.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a property selector (e.g., x => x.PropertyName)",
            nameof(propertySelector));
    }
}
