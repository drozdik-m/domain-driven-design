using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
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

internal class TestVoidCommand2Handler : ICommandHandler<TestUnitCommand2>
{
    public Task<UnitResult<Error>> HandleAsync(TestUnitCommand2 command, CancellationToken cancellationToken)
    {
        command.HandleDecrement--;
        return Task.FromResult(UnitResult.Success<Error>());
    }
}
