using System.Linq.Expressions;
using MartinDrozdik.DDD.Options;
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
    /// <example>
    /// x.A.B.C → ["C", "B", "A"].
    /// </example>
    /// <typeparam name="TOptions">The type of the <see cref="IAppOptions"/>.</typeparam>
    /// <param name="propertySelector">Expression to select the property.</param>
    /// <returns>Name of the selected property.</returns>
    private static string GetPropertyName<TOptions>(Expression<Func<TOptions, object>> propertySelector)
    {
        var parts = new List<string>();

        // Unwrap UnaryExpression (e.g. boxing conversions on value types)
        var current = propertySelector.Body is UnaryExpression unary
            ? unary.Operand
            : propertySelector.Body;

        // Walk up the member access chain: x.A.B.C → ["C", "B", "A"]
        while (current is MemberExpression member)
        {
            parts.Add(member.Member.Name);
            current = member.Expression;
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException(
                "Expression must be a property selector (e.g., x => x.Property)",
                nameof(propertySelector));
        }

        // Reverse so the order is outermost-first: ["A", "B", "C"]
        parts.Reverse();

        return string.Join(":", parts);
    }
}
