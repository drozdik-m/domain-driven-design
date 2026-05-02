using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class AndSpecificationTests
{
    [Fact]
    public void Should_throw_when_no_specifications_provided()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AndSpecification<object>());
    }

    [Fact]
    public void Should_be_satisfied_when_single_specification_is_satisfied()
    {
        var spec = new AndSpecification<object>(Satisfied());
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public void Should_not_be_satisfied_when_single_specification_is_not_satisfied()
    {
        var spec = new AndSpecification<object>(NotSatisfied(SomeError()));
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void Should_be_satisfied_when_all_specifications_are_satisfied()
    {
        var spec = new AndSpecification<object>(Satisfied(), Satisfied(), Satisfied());

        var result = spec.IsSatisfiedBy(new object());
        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_not_be_satisfied_when_first_specification_is_not_satisfied()
    {
        var error = SomeError();
        var spec = new AndSpecification<object>(NotSatisfied(error), Satisfied(), Satisfied());
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
        var single = Assert.Single(result.Errors);
        Assert.Equal(error, single);
    }

    [Fact]
    public void Should_not_be_satisfied_when_last_specification_is_not_satisfied()
    {
        var spec = new AndSpecification<object>(Satisfied(), Satisfied(), NotSatisfied(SomeError()));
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void Should_not_be_satisfied_when_all_specifications_are_not_satisfied()
    {
        var first = SomeError();
        var second = AnotherError();
        var spec = new AndSpecification<object>(NotSatisfied(first), NotSatisfied(second));
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(first, result.Errors);
        Assert.Contains(second, result.Errors);
    }

    private static Error SomeError() => new ErrorBuilder()
        .WithCode("TEST_ERROR")
        .WithMessage("Something was not satisfied")
        .Build();

    private static Error AnotherError() => new ErrorBuilder()
        .WithCode("ANOTHER_ERROR")
        .WithMessage("Something else was not satisfied")
        .Build();

    private static ISpecification<object> Satisfied()
        => new TestSpecification(SpecificationResult.Satisfied);

    private static ISpecification<object> NotSatisfied(Error error)
        => new TestSpecification(SpecificationResult.NotSatisfied(error));

    private sealed class TestSpecification(SpecificationResult result) : ISpecification<object>
    {
        public SpecificationResult IsSatisfiedBy(object context) => result;
    }
}
