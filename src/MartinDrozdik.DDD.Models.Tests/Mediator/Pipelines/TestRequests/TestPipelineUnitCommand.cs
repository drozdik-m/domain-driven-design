using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

internal record TestPipelineUnitCommand : ICommand
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

internal class TestPipelineUnitCommandHandler : ICommandHandler<TestPipelineUnitCommand>
{
    public Task HandleAsync(TestPipelineUnitCommand Command, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
