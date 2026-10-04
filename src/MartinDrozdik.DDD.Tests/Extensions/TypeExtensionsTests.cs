using System.Reflection;
using System.Reflection.Emit;
using MartinDrozdik.DDD.Extensions;

namespace MartinDrozdik.DDD.Tests.Extensions;

public class TypeExtensionsTests
{
    private static readonly Type s_listParameter = typeof(List<>).GetGenericArguments()[0];

    /// <summary>
    /// Gets every kind of type, with the name it is expected to get.
    /// </summary>
    public static TheoryData<Type, string> ExpectedNames => new()
    {
        // Nothing to spell out, so exactly Type.Name
        { typeof(TypeExtensionsTests), "TypeExtensionsTests" },
        { typeof(int), "Int32" },
        { typeof(string), "String" },
        { typeof(DayOfWeek), "DayOfWeek" },
        { s_listParameter, "T" },
        { typeof(Enumerable).GetMethod(nameof(Enumerable.Empty))!.GetGenericArguments()[0], "TResult" },

        // Closed generics
        { typeof(List<int>), "List<Int32>" },
        { typeof(Dictionary<string, List<int>>), "Dictionary<String, List<Int32>>" },
        { typeof(int?), "Nullable<Int32>" },
        { typeof((int, string)), "ValueTuple<Int32, String>" },
        { typeof(Func<int, Task<string>>), "Func<Int32, Task<String>>" },
        { typeof(IEnumerable<KeyValuePair<string, int[]>>), "IEnumerable<KeyValuePair<String, Int32[]>>" },

        // Open generic definitions show their parameters
        { typeof(List<>), "List<T>" },
        { typeof(Dictionary<,>), "Dictionary<TKey, TValue>" },

        // Arrays
        { typeof(int[]), "Int32[]" },
        { typeof(int[][]), "Int32[][]" },
        { typeof(int[,]), "Int32[,]" },
        { typeof(int[,,]), "Int32[,,]" },
        { typeof(int).MakeArrayType(1), "Int32[*]" },
        { typeof(List<int>[]), "List<Int32>[]" },
        { typeof(List<int>[,]), "List<Int32>[,]" },
        { typeof(List<int>[][]), "List<Int32>[][]" },
        { typeof(List<int>).MakeArrayType(1), "List<Int32>[*]" },
        { s_listParameter.MakeArrayType(), "T[]" },

        // Nested types are prefixed with their declaring types, each named with its own share of the generic arguments
        { typeof(PlainOuter.Inner), "TypeExtensionsTests.PlainOuter.Inner" },
        { typeof(PlainOuter.Inner<int>), "TypeExtensionsTests.PlainOuter.Inner<Int32>" },
        { typeof(GenericOuter<int>.Inner), "TypeExtensionsTests.GenericOuter<Int32>.Inner" },
        { typeof(GenericOuter<int>.Inner<string>), "TypeExtensionsTests.GenericOuter<Int32>.Inner<String>" },
        { typeof(GenericOuter<>.Inner<>), "TypeExtensionsTests.GenericOuter<TOuter>.Inner<TInner>" },
        { typeof(GenericOuter<int>.Middle.Deep<string>), "TypeExtensionsTests.GenericOuter<Int32>.Middle.Deep<String>" },
        { typeof(GenericOuter<int>.Inner<string>[]), "TypeExtensionsTests.GenericOuter<Int32>.Inner<String>[]" },
        { typeof(List<GenericOuter<int>.Inner>), "List<TypeExtensionsTests.GenericOuter<Int32>.Inner>" },
        { typeof(List<int>.Enumerator), "List<Int32>.Enumerator" },
        { typeof(Dictionary<string, int>.KeyCollection), "Dictionary<String, Int32>.KeyCollection" },

        // Pointers and by-refs are left alone
        { typeof(int).MakePointerType(), "Int32*" },
        { typeof(int).MakeByRefType(), "Int32&" },
        { typeof(List<int>).MakeByRefType(), "List`1&" },
    };

    [Theory]
    [MemberData(nameof(ExpectedNames))]
    public void Readable_name_spells_out_generic_arguments_and_nothing_else(Type type, string expected)
    {
        // Arrange
        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void Null_type_is_rejected()
    {
        // Arrange
        Type? type = null;

        // Act
        var exception = Record.Exception(() => type!.GetReadableName());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Anonymous_type_spells_out_its_property_types()
    {
        // Arrange
        var type = TypeOf(new { Number = 1, Text = "text" });

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.StartsWith("<>f__AnonymousType", name, StringComparison.Ordinal);
        Assert.EndsWith("<Int32, String>", name, StringComparison.Ordinal);
        Assert.DoesNotContain('`', name);
    }

    [Fact]
    public void Generic_type_emitted_at_runtime_spells_out_its_arguments()
    {
        // Arrange
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ReadableNameDynamic"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("Emitted`1", TypeAttributes.Public);
        var parameter = builder.DefineGenericParameters("TItem")[0];

        // Act
        var builderName = builder.GetReadableName();
        var parameterName = parameter.GetReadableName();
        var closedName = builder.CreateType().MakeGenericType(typeof(int)).GetReadableName();

        // Assert
        Assert.Equal("Emitted<TItem>", builderName);
        Assert.Equal("TItem", parameterName);
        Assert.Equal("Emitted<Int32>", closedName);
    }

    [Fact]
    public void Signature_types_never_throw()
    {
        // Arrange
        // Signature types throw NotSupportedException from much of the Type API
        Type[] types =
        [
            Type.MakeGenericMethodParameter(0),
            Type.MakeGenericMethodParameter(0).MakeArrayType(),
            Type.MakeGenericSignatureType(typeof(List<>), Type.MakeGenericMethodParameter(0)),
            Type.MakeGenericSignatureType(typeof(Dictionary<,>), typeof(int), Type.MakeGenericMethodParameter(1)).MakeArrayType(2),
        ];

        // Act
        var names = types.Select(type => type.GetReadableName()).ToList();

        // Assert
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void Function_pointer_is_named_by_ToString_as_its_name_is_empty()
    {
        // Arrange
        // Writing a function pointer type takes an unsafe context, so one is taken off a framework field
        var type = LoadableTypes(typeof(object).Assembly).SelectMany(FieldTypes).First(candidate => candidate.IsFunctionPointer);

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal(type.ToString(), name);
    }

    [Fact]
    public void Failing_generic_arguments_fall_back_to_the_plain_name()
    {
        // Arrange
        var type = new FakeType
        {
            Generic = true,
            NameFactory = () => "Broken`1",
            GenericArgumentsFactory = () => throw new InvalidOperationException("Reflection gave up."),
        };

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal("Broken`1", name);
    }

    [Fact]
    public void Failing_declaring_type_falls_back_to_the_plain_name()
    {
        // Arrange
        var type = new FakeType
        {
            Generic = true,
            NameFactory = () => "Broken`1",
            GenericArgumentsFactory = () => [typeof(int)],
            DeclaringTypeFactory = () => throw new NotSupportedException("No declaring type here."),
        };

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal("Broken`1", name);
    }

    [Fact]
    public void Generic_type_without_arguments_gets_its_bare_name()
    {
        // Arrange
        var type = new FakeType { Generic = true, NameFactory = () => "Broken`1", GenericArgumentsFactory = () => [] };

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal("Broken", name);
    }

    [Fact]
    public void Array_without_an_element_type_falls_back_to_the_plain_name()
    {
        // Arrange
        var type = new FakeType { Array = true, NameFactory = () => "Weird[]", ElementTypeFactory = () => null };

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal("Weird[]", name);
    }

    [Fact]
    public void Array_with_a_failing_element_type_falls_back_to_the_plain_name()
    {
        // Arrange
        var type = new FakeType
        {
            Array = true,
            NameFactory = () => "Weird[]",
            ElementTypeFactory = () => throw new NotSupportedException("No element."),
        };

        // Act
        var name = type.GetReadableName();

        // Assert
        Assert.Equal("Weird[]", name);
    }

    [Fact]
    public void Type_declared_inside_itself_gets_its_plain_name_instead_of_overflowing_the_stack()
    {
        // Arrange
        FakeType? looped = null;
        looped = new FakeType { NameFactory = () => "Looped", DeclaringTypeFactory = () => looped };

        // Act
        var name = looped.GetReadableName();

        // Assert
        Assert.Equal("Looped", name);
    }

    [Fact]
    public void Type_listing_itself_as_its_own_argument_gets_its_plain_name_instead_of_overflowing_the_stack()
    {
        // Arrange
        FakeType? cyclic = null;
        cyclic = new FakeType { Generic = true, NameFactory = () => "Cyclic`1", GenericArgumentsFactory = () => [cyclic!] };

        // Act
        var name = cyclic.GetReadableName();

        // Assert
        Assert.Equal("Cyclic`1", name);
    }

    [Fact]
    public void Type_listing_itself_as_both_of_its_arguments_gets_its_plain_name_instead_of_running_for_hours()
    {
        // Arrange
        // Two arguments double the work on every level, so the depth limit has to give up on the whole name, not just on the branch that hit it.
        // Past the budget the type stops listing arguments, so that the test fails instead of hanging.
        const int budget = 100_000;
        var reads = 0;
        FakeType? forked = null;
        forked = new FakeType
        {
            Generic = true,
            NameFactory = () => "Forked`2",
            GenericArgumentsFactory = () => ++reads > budget ? [] : [forked!, forked!],
        };

        // Act
        var name = forked.GetReadableName();

        // Assert
        Assert.True(reads <= budget, $"The generic arguments were read {reads} times.");
        Assert.Equal("Forked`2", name);
    }

    [Fact]
    public void Every_type_of_the_framework_and_this_library_gets_a_readable_name()
    {
        // Arrange
        Assembly[] assemblies =
        [
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(StringExtensions).Assembly,
            typeof(TypeExtensionsTests).Assembly,
        ];

        // Field types add what no assembly defines on its own: function pointers, pointers, closed generics in use
        var defined = assemblies.SelectMany(LoadableTypes).ToList();
        var types = defined.SelectMany(Variants).Concat(defined.SelectMany(FieldTypes)).ToList();
        var failures = new List<string>();

        // Act
        foreach (var type in types)
        {
            try
            {
                var name = type.GetReadableName();
                if (string.IsNullOrWhiteSpace(name))
                {
                    failures.Add($"{type}: empty name");
                }
                else if (IsNamedAsItIs(type))
                {
                    // Pointers and by-refs keep whatever Type.Name gives them, backticks included, and function pointers their ToString()
                }
                else if (name.Contains('`', StringComparison.Ordinal))
                {
                    failures.Add($"{type}: '{name}'");
                }
                else if (!type.IsGenericType && !type.IsArray && !type.IsNested && name != type.Name)
                {
                    // Nothing to spell out, so the name must not change at all
                    failures.Add($"{type}: '{name}' instead of '{type.Name}'");
                }
                else if (!type.IsGenericType && !type.IsArray && type.IsNested && !type.IsGenericParameter && !name.EndsWith($".{type.Name}", StringComparison.Ordinal))
                {
                    // Only the declaring types are added in front
                    failures.Add($"{type}: '{name}' does not end with '.{type.Name}'");
                }
            }
#pragma warning disable CA1031 // Catch a more specific exception - any exception at all is the failure being looked for
            catch (Exception exception)
#pragma warning restore CA1031
            {
                failures.Add($"{type}: {exception.GetType().Name} {exception.Message}");
            }
        }

        // Assert
        Assert.True(types.Count > 1_000, $"Only {types.Count} types were checked.");
        Assert.True(failures.Count == 0, $"{failures.Count} type(s) failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(50))}");
    }

    private static Type TypeOf<T>(T value) => value?.GetType() ?? typeof(T);

    /// <summary>
    /// Decides whether <paramref name="type"/>, or the element type its arrays end in, is one that keeps its plain name.
    /// </summary>
    /// <param name="type">The checked type.</param>
    /// <returns><see langword="true"/> for pointers, by-refs and function pointers.</returns>
    private static bool IsNamedAsItIs(Type type)
    {
        var element = type;
        while (element.IsArray && element.GetElementType() is { } inner)
        {
            element = inner;
        }

        return element.IsPointer || element.IsByRef || element.IsFunctionPointer;
    }

    private static IEnumerable<Type> FieldTypes(Type type)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        FieldInfo[] fields;
        try
        {
            fields = type.GetFields(all);
        }
#pragma warning disable CA1031 // Catch a more specific exception - a type whose fields cannot be loaded simply adds none
        catch (Exception)
#pragma warning restore CA1031
        {
            return [];
        }

        return fields.Select(field => TryMake(() => field.FieldType)).OfType<Type>();
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    /// <summary>
    /// The type itself, plus the arrays and closed generics that can be made of it.
    /// </summary>
    /// <param name="type">A type defined in an assembly.</param>
    /// <returns>The type and every variant of it the runtime agrees to make.</returns>
    private static IEnumerable<Type> Variants(Type type)
    {
        yield return type;

        foreach (var make in new Func<Type>[]
        {
            () => type.MakeArrayType(),
            () => type.MakeArrayType(2),
            () => type.MakeGenericType(Enumerable.Repeat(typeof(object), type.GetGenericArguments().Length).ToArray()),
        })
        {
            var variant = TryMake(make);
            if (variant is not null)
            {
                yield return variant;
            }
        }
    }

    private static Type? TryMake(Func<Type> make)
    {
        try
        {
            return make();
        }
#pragma warning disable CA1031 // Catch a more specific exception - a variant the runtime refuses to make (ref struct arrays, unmet constraints, ...) is simply skipped
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

#pragma warning disable S2326, S2094 // Unused type parameters, empty classes - only their names are under test
    private static class PlainOuter
    {
        internal sealed class Inner
        {
        }

        internal sealed class Inner<TInner>
        {
        }
    }

    private static class GenericOuter<TOuter>
    {
        internal static class Middle
        {
            internal sealed class Deep<TDeep>
            {
            }
        }

        internal sealed class Inner
        {
        }

        internal sealed class Inner<TInner>
        {
        }
    }
#pragma warning restore S2326, S2094

    /// <summary>
    /// A <see cref="Type"/> that can be made to misbehave in every way the readable name relies on.
    /// </summary>
    private sealed class FakeType() : TypeDelegator(typeof(object))
    {
        public Func<string> NameFactory { get; init; } = () => "FakeType";

        public Func<Type[]> GenericArgumentsFactory { get; init; } = () => [];

        public Func<Type?> ElementTypeFactory { get; init; } = () => null;

        public Func<Type?> DeclaringTypeFactory { get; init; } = () => null;

        public bool Generic { get; init; }

        public bool Array { get; init; }

        public override string Name => NameFactory();

        public override bool IsGenericType => Generic;

        public override Type? DeclaringType => DeclaringTypeFactory();

        public override Type[] GetGenericArguments() => GenericArgumentsFactory();

        public override Type? GetElementType() => ElementTypeFactory();

        protected override bool IsArrayImpl() => Array;

        protected override bool HasElementTypeImpl() => Array;
    }
}
