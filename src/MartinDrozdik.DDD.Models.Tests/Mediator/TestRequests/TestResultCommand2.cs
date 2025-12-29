using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;

internal record TestResultCommand2(int Result) : ICommand<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        result.IsSuccess();
        Assert.Equal(Result, result.Value);
    }
}

internal class TestResultCommand2Handler : ICommandHandler<TestResultCommand2, int>
{
    public Task<int> HandleAsync(TestResultCommand2 command, CancellationToken cancellationToken)
    {
        return Task.FromResult(command.Result);
    }
}
