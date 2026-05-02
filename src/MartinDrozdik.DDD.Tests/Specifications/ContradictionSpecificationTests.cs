using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class ContradictionSpecificationTests
{
    [Fact]
    public void Should_never_be_satisfied()
    {
        var error = SomeError();
        var spec = new ContradictionSpecification<object>(SomeError());
        var result = spec.IsSatisfiedBy(new object());

        Assert.False(result.IsSatisfied);
        var single = Assert.Single(result.Errors);
        Assert.Equal(error, single);
    }

    private static Error SomeError() => new ErrorBuilder()
        .WithCode("TEST_ERROR")
        .WithMessage("Something was not satisfied")
        .Build();
}
