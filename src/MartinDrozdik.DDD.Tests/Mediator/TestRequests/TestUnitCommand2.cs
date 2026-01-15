using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;

internal class TestUnitCommand2 : ICommand
{
    public int HandleDecrement { get; set; }

    public void AssertHandled(int handleCount = 1)
    {
        Assert.Equal(-handleCount, HandleDecrement);
    }
}

internal class TestUnitCommand2Handler : ICommandHandler<TestUnitCommand2>
{
    public Task HandleAsync(TestUnitCommand2 command, CancellationToken cancellationToken)
    {
        command.HandleDecrement--;
        return Task.CompletedTask;
    }
}
