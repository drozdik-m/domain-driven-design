using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class SpecificationResultTests
{
    [Fact]
    public void Satisfied_should_be_satisfied_with_no_errors()
    {
        var result = SpecificationResult.Satisfied;

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Satisfied_should_be_true()
    {
        var result = SpecificationResult.Satisfied;

        Assert.True((bool)result);
    }

    [Fact]
    public void Should_not_be_satisfied_with_errors()
    {
        var error = SomeError();
        var result = SpecificationResult.NotSatisfied(error);

        Assert.False(result.IsSatisfied);
        var single = Assert.Single(result.Errors);
        Assert.Equal(error, single);
    }

    [Fact]
    public void Should_contain_all_provided_errors()
    {
        var errors = new[] { SomeError(), AnotherError() };
        var result = SpecificationResult.NotSatisfied(errors);

        Assert.Equal(2, result.Errors.Count);
        Assert.True(errors.All(e => result.Errors.Contains(e)));
    }

    [Fact]
    public void Should_throw_when_no_errors_provided()
    {
        Assert.Throws<ArgumentException>(() => SpecificationResult.NotSatisfied([]));
    }

    [Fact]
    public void NotSatisfied_should_be_false()
    {
        var result = SpecificationResult.NotSatisfied(SomeError());

        Assert.False((bool)result);
    }

    [Fact]
    public void And_operator_works()
    {
        var result = SpecificationResult.Satisfied & SpecificationResult.Satisfied;
        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public void And_should_not_be_satisfied_when_left_is_not_satisfied()
    {
        var result = SpecificationResult.NotSatisfied(SomeError()) & SpecificationResult.Satisfied;
        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void And_should_not_be_satisfied_when_right_is_not_satisfied()
    {
        var result = SpecificationResult.Satisfied & SpecificationResult.NotSatisfied(SomeError());
        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void And_should_not_be_satisfied_when_both_are_not_satisfied()
    {
        var result = SpecificationResult.NotSatisfied(SomeError()) & SpecificationResult.NotSatisfied(AnotherError());
        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void And_should_aggregate_errors_from_both_sides_when_both_fail()
    {
        var left = SpecificationResult.NotSatisfied(SomeError());
        var right = SpecificationResult.NotSatisfied(AnotherError());

        var result = left & right;

        Assert.Equal(2, result.Errors.Count);
        Assert.True(left.Errors.All(e => result.Errors.Contains(e)));
        Assert.True(right.Errors.All(e => result.Errors.Contains(e)));
    }

    [Fact]
    public void And_should_return_only_left_errors_when_only_left_fails()
    {
        var left = SpecificationResult.NotSatisfied(SomeError());
        var right = SpecificationResult.Satisfied;

        var result = left & right;

        Assert.Equal(left.Errors, result.Errors);
    }

    [Fact]
    public void And_should_return_only_right_errors_when_only_right_fails()
    {
        var left = SpecificationResult.Satisfied;
        var right = SpecificationResult.NotSatisfied(SomeError());

        var result = left & right;

        Assert.Equal(right.Errors, result.Errors);
    }

    [Fact]
    public void Or_should_be_satisfied_when_both_are_satisfied()
    {
        var result = SpecificationResult.Satisfied | SpecificationResult.Satisfied;

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public void Or_should_be_satisfied_when_left_is_satisfied()
    {
        var result = SpecificationResult.Satisfied | SpecificationResult.NotSatisfied(SomeError());

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public void Or_should_be_satisfied_when_right_is_satisfied()
    {
        var result = SpecificationResult.NotSatisfied(SomeError()) | SpecificationResult.Satisfied;

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public void Or_should_not_be_satisfied_when_both_are_not_satisfied()
    {
        var result = SpecificationResult.NotSatisfied(SomeError()) | SpecificationResult.NotSatisfied(AnotherError());

        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void Or_should_aggregate_errors_from_both_sides_when_both_fail()
    {
        var left = SpecificationResult.NotSatisfied(SomeError());
        var right = SpecificationResult.NotSatisfied(AnotherError());

        var result = left | right;

        Assert.Equal(2, result.Errors.Count);
        Assert.True(left.Errors.All(e => result.Errors.Contains(e)));
        Assert.True(right.Errors.All(e => result.Errors.Contains(e)));
    }

    [Fact]
    public void ShortCircuitAnd_should_be_satisfied_when_both_are_satisfied()
    {
        var left = SpecificationResult.Satisfied;
        var right = SpecificationResult.Satisfied;

        Assert.True(left && right);
    }

    [Fact]
    public void ShortCircuitAnd_should_not_be_satisfied_when_left_is_not_satisfied()
    {
        var left = SpecificationResult.NotSatisfied(SomeError());
        var right = SpecificationResult.Satisfied;

        Assert.False(left && right);
    }

    [Fact]
    public void ShortCircuitOr_should_be_satisfied_when_left_is_satisfied()
    {
        var left = SpecificationResult.Satisfied;
        var right = SpecificationResult.NotSatisfied(SomeError());

        Assert.True(left || right);
    }

    [Fact]
    public void ShortCircuitOr_should_not_be_satisfied_when_both_are_not_satisfied()
    {
        var left = SpecificationResult.NotSatisfied(SomeError());
        var right = SpecificationResult.NotSatisfied(AnotherError());

        Assert.False(left || right);
    }

    private static Error SomeError() => new ErrorBuilder()
        .WithCode("TEST_ERROR")
        .WithMessage("Something was not satisfied")
        .Build();

    private static Error AnotherError() => new ErrorBuilder()
        .WithCode("ANOTHER_ERROR")
        .WithMessage("Something else was not satisfied")
        .Build();
}
