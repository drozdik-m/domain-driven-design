using MartinDrozdik.DDD.Identities.Converters;
using MartinDrozdik.DDD.Identities.Primitive;

namespace MartinDrozdik.DDD.Tests.Identities.Converters;

public class IdentityConverterTests
{
    [Fact]
    public void CreateGuid_converts_both_ways()
    {
        // Arrange
        var key = Guid.CreateVersion7();
        var converter = IdentityConverter.CreateGuid(value => new GuidId(value));

        // Act
        var identity = converter.FromKey(key);
        var roundTripped = converter.ToKey(identity);

        // Assert
        Assert.Equal(new GuidId(key), identity);
        Assert.Equal(key, roundTripped);
    }

    [Fact]
    public void CreateInt_converts_both_ways()
    {
        // Arrange
        var converter = IdentityConverter.CreateInt(value => new IntId(value));

        // Act
        var identity = converter.FromKey(42);
        var roundTripped = converter.ToKey(identity);

        // Assert
        Assert.Equal(new IntId(42), identity);
        Assert.Equal(42, roundTripped);
    }

    [Fact]
    public void CreateString_converts_both_ways()
    {
        // Arrange
        var converter = IdentityConverter.CreateString(value => new StringId(value));

        // Act
        var identity = converter.FromKey("ABC-123");
        var roundTripped = converter.ToKey(identity);

        // Assert
        Assert.Equal(new StringId("ABC-123"), identity);
        Assert.Equal("ABC-123", roundTripped);
    }

    [Fact]
    public void Factories_expose_expressions_that_compile_to_the_same_conversion()
    {
        // Arrange
        var converter = IdentityConverter.CreateInt(value => new IntId(value));

        // Act
        var identity = converter.FromKeyExpression.Compile()(7);
        var key = converter.ToKeyExpression.Compile()(identity);

        // Assert
        Assert.Equal(new IntId(7), identity);
        Assert.Equal(7, key);
    }

    [Fact]
    public void Factory_rejects_a_null_expression()
    {
        // Arrange
        // Act
        var exception = Record.Exception(() => IdentityConverter.CreateString<StringId>(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    private sealed class IntId(int key) : IntIdentity<IntId>(key)
    {
    }

    private sealed class StringId(string key) : StringIdentity<StringId>(key)
    {
    }

    private sealed class GuidId(Guid key) : GuidIdentity<GuidId>(key)
    {
    }
}
