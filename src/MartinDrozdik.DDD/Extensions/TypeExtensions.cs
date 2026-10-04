using System.Reflection;

namespace MartinDrozdik.DDD.Extensions;

/// <summary>
/// Extensions for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// How deep generic arguments, array elements and declaring types are followed.
    /// </summary>
    /// <remarks>
    /// Real types never come close. It only gives a malformed <see cref="Type"/> that refers to itself its plain name instead of overflowing the stack.
    /// </remarks>
    private const int MaxDepth = 32;

    /// <summary>
    /// Gets a readable name of <paramref name="type"/> for log, exception and validation messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Same as <see cref="MemberInfo.Name"/>, except that generic arguments are spelled out instead of the arity suffix, and nested types are prefixed with their declaring types.
    /// </para>
    /// <para>
    /// Pointers, by-refs and generic parameters keep their <see cref="MemberInfo.Name"/>, and so does a type the formatting fails on, such as a modified or signature type.
    /// Where the name is empty, as for function pointers, <see cref="Type.ToString"/> is used instead.
    /// </para>
    /// </remarks>
    /// <param name="type">The type to name.</param>
    /// <returns>The readable name.</returns>
    /// <example><c>typeof(Dictionary&lt;string, List&lt;int&gt;&gt;).GetReadableName()</c> returns <c>Dictionary&lt;String, List&lt;Int32&gt;&gt;</c>.</example>
    public static string GetReadableName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        try
        {
            return Format(type, depth: 0);
        }
#pragma warning disable CA1031, S2221, RCS1075 // Reflection over an unusual type can throw almost anything
        catch (Exception)
#pragma warning restore CA1031, S2221, RCS1075
        {
            return PlainName(type);
        }
    }

    /// <summary>
    /// Formats <paramref name="type"/>, spelling out generic arguments and the element types of arrays.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="depth">How many generic arguments, array elements or declaring types deep this type sits.</param>
    /// <returns>The formatted name.</returns>
    private static string Format(Type type, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, MaxDepth);

        if (type.IsArray)
        {
            // The name ends with the brackets right after the name of the element: [], [,] or [*]
            var element = type.GetElementType()!;
            return Format(element, depth + 1) + type.Name[element.Name.Length..];
        }

        if (type.HasElementType || type.IsFunctionPointer || type.IsGenericParameter)
        {
            return PlainName(type);
        }

        var arguments = type.IsGenericType
            ? type.GetGenericArguments()
            : Type.EmptyTypes;
        return FormatNamed(type, arguments, depth);
    }

    /// <summary>
    /// Formats a class, struct, interface or delegate, prefixed with its declaring types.
    /// </summary>
    /// <param name="type">The type to format. For a declaring type, the open generic definition.</param>
    /// <param name="arguments">The generic arguments <paramref name="type"/> is closed with, including those of its declaring types.</param>
    /// <param name="depth">How many generic arguments, array elements or declaring types deep this type sits.</param>
    /// <returns>The formatted name.</returns>
    private static string FormatNamed(Type type, Type[] arguments, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, MaxDepth);

        // Nested types list the generic arguments of their declaring types first, as many as the declaring type has parameters
        var declaring = type.DeclaringType;
        var inherited = declaring?.GetGenericArguments().Length ?? 0;
        var prefix = declaring is null
            ? string.Empty
            : FormatNamed(declaring, arguments[..inherited], depth + 1) + ".";

        var name = PlainName(type).Split('`')[0];
        var own = arguments[inherited..];
        return own.Length == 0
            ? prefix + name
            : $"{prefix}{name}<{string.Join(", ", own.Select(argument => Format(argument, depth + 1)))}>";
    }

    /// <summary>
    /// Gets <see cref="MemberInfo.Name"/>, or <see cref="Type.ToString"/> when the name is empty.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The plain name.</returns>
    private static string PlainName(Type type) =>
        string.IsNullOrWhiteSpace(type.Name) ? type.ToString() : type.Name;
}
