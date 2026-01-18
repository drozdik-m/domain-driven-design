using MartinDrozdik.DDD.Mediator.Commands;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;

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
    public Task<int> HandleAsync(TestPipelineCommand Command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Command.Result);
    }
}
