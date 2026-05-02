using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class NotSpecificationTests
{
    [Fact]
    public void Should_be_satisfied_when_inner_specification_is_not_satisfied()
    {
        var spec = new NotSpecification<object>(NotSatisfied(SomeError()), NegationError());
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_not_be_satisfied_when_inner_specification_is_satisfied()
    {
        var error = NegationError();
        var spec = new NotSpecification<object>(Satisfied(), error);
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);

        var single = Assert.Single(result.Errors);
        Assert.Equal(error, single);
    }

    private static Error SomeError() => new ErrorBuilder()
        .WithCode("TEST_ERROR")
        .WithMessage("Something was not satisfied")
        .Build();

    private static Error NegationError() => new ErrorBuilder()
        .WithCode("NEGATION_ERROR")
        .WithMessage("Negated specification was satisfied")
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
