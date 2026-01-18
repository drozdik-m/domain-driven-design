using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Mediator.Queries;
using MartinDrozdik.DDD.Tests;

namespace MartinDrozdik.DDD.Tests.Mediator.TestRequests;

internal record TestQuery2(int Result) : IQuery<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        result.IsSuccess();
        Assert.Equal(Result, result.Value);
    }
}

internal class TestQuery2Handler : IQueryHandler<TestQuery2, int>
{
    public Task<int> HandleAsync(TestQuery2 query, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.Result);
    }
}
