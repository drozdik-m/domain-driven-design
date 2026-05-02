using MartinDrozdik.DDD.Specifications;

namespace MartinDrozdik.DDD.Tests.Specifications;

public class TautologySpecificationTests
{
    [Fact]
    public void Should_always_be_satisfied()
    {
        var spec = TautologySpecification<object>.Instance;
        var result = spec.IsSatisfiedBy(new object());

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Errors);
    }
}
