using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Requests;

internal record TestResultCommand1(int Result) : ICommand<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        ResultAssert.IsSuccess(result);
        Assert.Equal(Result, result.Value);
    }
}

internal class TestResultCommand1Handler : ICommandHandler<TestResultCommand1, int>
{
    public Task<Result<int, Error>> HandleAsync(TestResultCommand1 command, CancellationToken cancellationToken)
    {
        var result = Result.Success<int, Error>(command.Result);
        return Task.FromResult(result);
    }
}
