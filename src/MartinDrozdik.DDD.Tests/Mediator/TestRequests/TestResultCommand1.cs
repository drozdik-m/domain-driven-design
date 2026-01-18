using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Errors;
using MartinDrozdik.DDD.Mediator.Commands;

namespace MartinDrozdik.DDD.Tests.Mediator.TestRequests;

internal record TestResultCommand1(int Result) : ICommand<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        result.IsSuccess();
        Assert.Equal(Result, result.Value);
    }
}

internal class TestResultCommand1Handler : ICommandHandler<TestResultCommand1, int>
{
    public Task<int> HandleAsync(TestResultCommand1 command, CancellationToken cancellationToken)
    {
        return Task.FromResult(command.Result);
    }
}
