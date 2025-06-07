using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Queries;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Requests;

internal record TestQuery1(int Result) : IQuery<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        ResultAssert.IsSuccess(result);
        Assert.Equal(Result, result.Value);
    }
}

internal class TestQuery1Handler : IQueryHandler<TestQuery1, int>
{
    public Task<Result<int, Error>> HandleAsync(TestQuery1 query, CancellationToken cancellationToken)
    {
        var result = Result.Success<int, Error>(query.Result);
        return Task.FromResult(result);
    }
}
