using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;

internal class TestVoidCommand1 : ICommand
{
    public int HandleIncrement { get; set; }

    public void AssertHandled(int handleCount = 1)
    {
        Assert.Equal(handleCount, HandleIncrement);
    }
}

internal class TestVoidCommand1Handler : ICommandHandler<TestVoidCommand1>
{
    public Task<UnitResult<Error>> HandleAsync(TestVoidCommand1 command, CancellationToken cancellationToken)
    {
        command.HandleIncrement++;
        return Task.FromResult(UnitResult.Success<Error>());
    }
}
