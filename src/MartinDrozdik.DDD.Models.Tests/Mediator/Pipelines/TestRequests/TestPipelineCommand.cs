using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

internal record TestPipelineCommand(int Result) : ICommand<int>
{
    private readonly List<string> _callStack = [];

    public void AddCall(string id)
    {
        _callStack.Add(id);
    }

    public void AssertCallStack(params string[] expected)
    {
        Assert.Equal(expected, _callStack);
    }
}

internal class TestPipelineCommandHandler : ICommandHandler<TestPipelineCommand, int>
{
    public Task<Result<int, Error>> HandleAsync(TestPipelineCommand Command, CancellationToken cancellationToken)
    {
        var result = Result.Success<int, Error>(Command.Result);
        return Task.FromResult(result);
    }
}
