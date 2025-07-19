using MartinDrozdik.DDD.Models.Mediator;
using MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Tests.Mediator;

public class ServiceMediatorTests
{
    [Fact]
    public async Task Void_command_is_sent_and_handled()
    {
        // Arrange
        var mediator = CreateMediatorWithHandlers();
        var command1 = new TestVoidCommand1();
        var command2 = new TestVoidCommand2();

        // Act
        var result1 = await mediator.SendCommand(command1, CancellationToken.None);
        var result2 = await mediator.SendCommand(command2, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => ResultAssert.IsSuccess(result1),
            () => ResultAssert.IsSuccess(result2),
            () => command1.AssertHandled(),
            () => command2.AssertHandled());
    }

    [Fact]
    public async Task Result_command_is_sent_and_handled()
    {
        // Arrange
        var mediator = CreateMediatorWithHandlers();
        var command1 = new TestResultCommand1(1);
        var command2 = new TestResultCommand2(2);

        // Act
        var result1 = await mediator.SendCommand<TestResultCommand1, int>(command1, CancellationToken.None);
        var result2 = await mediator.SendCommand<TestResultCommand2, int>(command2, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => command1.AssertHandled(result1),
            () => command2.AssertHandled(result2));
    }

    [Fact]
    public async Task Query_is_sent_and_handled()
    {
        // Arrange
        var mediator = CreateMediatorWithHandlers();
        var query1 = new TestQuery1(1);
        var query2 = new TestQuery2(2);

        // Act
        var result1 = await mediator.SendQuery<TestQuery1, int>(query1, CancellationToken.None);
        var result2 = await mediator.SendQuery<TestQuery2, int>(query2, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => query1.AssertHandled(result1),
            () => query2.AssertHandled(result2));
    }

    private static ServiceMediator CreateMediatorWithHandlers()
    {
        var services = new ServiceCollection();
        services.AddTestRequests();
        var provider = services.BuildServiceProvider();
        return new ServiceMediator(provider);
    }
}
