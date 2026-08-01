using MartinDrozdik.DDD.Identities.Primitive;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Identities.Primitive;

public class PrimitiveIdentityTests
{
    [Fact]
    public void IntIdentity_can_be_derived_with_primary_constructor_and_exposes_key()
    {
        // Arrange
        var id = new IntId(42);

        // Act
        var key = id.Key;

        // Assert
        Assert.Equal(42, key);
    }

    [Fact]
    public void StringIdentity_can_be_derived_with_primary_constructor_and_exposes_key()
    {
        // Arrange
        var id = new StringId("ABC-123");

        // Act
        var key = id.Key;

        // Assert
        Assert.Equal("ABC-123", key);
    }

    [Fact]
    public void GuidIdentity_can_be_derived_with_primary_constructor_and_exposes_key()
    {
        // Arrange
        var guid = Guid.CreateVersion7();
        var id = new GuidId(guid);

        // Act
        var key = id.Key;

        // Assert
        Assert.Equal(guid, key);
    }

    [Fact]
    public void IntIdentity_compares_by_value()
    {
        // Arrange
        var id1 = new IntId(1);
        var id2 = new IntId(1);
        var different = new IntId(2);

        // Act & Assert
        EqualityAssert.TestEqualityComparer(comparer: id1, id1, id2, different);
        EqualityAssert.TestEqualityOperators<ValueObject>(id1, id2, different);
    }

    [Fact]
    public void StringIdentity_compares_by_value()
    {
        // Arrange
        var id1 = new StringId("same");
        var id2 = new StringId("same");
        var different = new StringId("other");

        // Act & Assert
        EqualityAssert.TestEqualityComparer(comparer: id1, id1, id2, different);
        EqualityAssert.TestEqualityOperators<ValueObject>(id1, id2, different);
    }

    [Fact]
    public void GuidIdentity_compares_by_value()
    {
        // Arrange
        var guid = Guid.CreateVersion7();
        var id1 = new GuidId(guid);
        var id2 = new GuidId(guid);
        var different = new GuidId(Guid.CreateVersion7());

        // Act & Assert
        EqualityAssert.TestEqualityComparer(comparer: id1, id1, id2, different);
        EqualityAssert.TestEqualityOperators<ValueObject>(id1, id2, different);
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
