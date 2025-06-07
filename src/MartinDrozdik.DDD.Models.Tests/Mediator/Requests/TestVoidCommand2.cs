using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Requests;

internal class TestVoidCommand2 : ICommand
{
    public int HandleDecrement { get; set; }

    public void AssertHandled(int handleCount = 1)
    {
        Assert.Equal(-handleCount, HandleDecrement);
    }
}

internal class TestVoidCommand2Handler : ICommandHandler<TestVoidCommand2>
{
    public Task<UnitResult<Error>> HandleAsync(TestVoidCommand2 command, CancellationToken cancellationToken)
    {
        command.HandleDecrement--;
        return Task.FromResult(UnitResult.Success<Error>());
    }
}
