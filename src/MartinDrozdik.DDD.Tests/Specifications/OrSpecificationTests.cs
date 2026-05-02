using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class OrSpecificationTests
{
    [Fact]
    public void Constructor_should_throw_when_no_specifications_provided()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrSpecification<object>());
    }

    [Fact]
    public void Should_be_satisfied_when_single_specification_is_satisfied()
    {
        var spec = new OrSpecification<object>(Satisfied());
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_not_be_satisfied_when_single_specification_is_not_satisfied()
    {
        var error = SomeError();
        var spec = new OrSpecification<object>(NotSatisfied(error));
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
        var single = Assert.Single(result.Errors);
        Assert.Equal(error, single);
    }

    [Fact]
    public void Should_be_satisfied_when_all_specifications_are_satisfied()
    {
        var spec = new OrSpecification<object>(Satisfied(), Satisfied(), Satisfied());
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_be_satisfied_when_first_specification_is_satisfied()
    {
        var spec = new OrSpecification<object>(Satisfied(), NotSatisfied(SomeError()), NotSatisfied(AnotherError()));
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_be_satisfied_when_last_specification_is_satisfied()
    {
        var spec = new OrSpecification<object>(NotSatisfied(SomeError()), NotSatisfied(AnotherError()), Satisfied());
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_not_be_satisfied_when_all_specifications_are_not_satisfied()
    {
        var first = SomeError();
        var second = AnotherError();
        var spec = new OrSpecification<object>(NotSatisfied(first), NotSatisfied(second));
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
