using MartinDrozdik.DDD.Templates;

namespace MartinDrozdik.DDD.Tests.Templates;

public class TypeExtensionsTests
{
    private interface ISomeInterface
    {
    }

    [Fact]
    public void IsAggregateRoot_returns_true_for_aggregate_root()
    {
        Assert.True(typeof(PlainAggregate).IsAggregateRoot());
    }

    [Fact]
    public void IsAggregateRoot_returns_false_for_plain_entity()
    {
        Assert.False(typeof(PlainEntity).IsAggregateRoot());
    }

    [Fact]
    public void IsAggregateRoot_returns_false_for_plain_class()
    {
        Assert.False(typeof(PlainClass).IsAggregateRoot());
    }

    [Fact]
    public void IsAggregateRoot_returns_false_for_arbitrary_interface_implementation()
    {
        Assert.False(typeof(ImplementsArbitraryInterface).IsAggregateRoot());
    }

    [Fact]
    public void IsAggregateRoot_returns_true_when_type_implements_both_entity_and_aggregate_root()
    {
        Assert.True(typeof(EntityAndAggregate).IsAggregateRoot());
    }

    [Fact]
    public void IsAggregateRoot_returns_false_for_interface_itself()
    {
        Assert.False(typeof(IAggregateRoot<int>).IsAggregateRoot());
    }

    [Fact]
    public void IsDomainEntity_returns_true_for_domain_entity()
    {
        Assert.True(typeof(PlainEntity).IsDomainEntity());
    }

    [Fact]
    public void IsDomainEntity_returns_false_for_plain_aggregate()
    {
        Assert.False(typeof(PlainAggregate).IsDomainEntity());
    }

    [Fact]
    public void IsDomainEntity_returns_false_for_plain_class()
    {
        Assert.False(typeof(PlainClass).IsDomainEntity());
    }

    [Fact]
    public void IsDomainEntity_returns_false_for_arbitrary_interface_implementation()
    {
        Assert.False(typeof(ImplementsArbitraryInterface).IsDomainEntity());
    }

    [Fact]
    public void IsDomainEntity_returns_true_when_type_implements_both_entity_and_aggregate_root()
    {
        Assert.True(typeof(EntityAndAggregate).IsDomainEntity());
    }

    [Fact]
    public void IsDomainEntity_returns_false_for_interface_itself()
    {
        Assert.False(typeof(IDomainEntity<int>).IsDomainEntity());
    }

    private class PlainAggregate : IAggregateRoot<int>
    {
        public int Id { get; } = 1;
    }

    private class PlainEntity : IDomainEntity<int>
    {
        public int Id { get; } = 1;
    }

#pragma warning disable S1939 // Inheritance list should not be redundant
    private class EntityAndAggregate : IDomainEntity<int>, IAggregateRoot<int>
    {
        public int Id { get; } = 1;
    }
#pragma warning restore S1939 // Inheritance list should not be redundant

#pragma warning disable S2094 // Classes should not be empty
    private class PlainClass
    {
    }
#pragma warning restore S2094 // Classes should not be empty

    private class ImplementsArbitraryInterface : ISomeInterface
    {
    }
}
