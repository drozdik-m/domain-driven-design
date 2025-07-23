using MartinDrozdik.DDD.Models.Mediator;
using MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Tests.Mediator;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task Manual_registrations_work_correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithQuery<TestQuery1, int, TestQuery1Handler>();
            builder.WithCommand<TestResultCommand1, int, TestResultCommand1Handler>();
            builder.WithCommand<TestUnitCommand1, TestUnitCommand1Handler>();
        });

        // Act & Assert
        await RunTestRequests(services);
    }

    [Fact]
    public async Task Assembly_registrations_work_correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(builder =>
        {
            builder.WithRequestsFromAssembly<ServiceCollectionExtensionsTests>();
        });

        // Act & Assert
        await RunTestRequests(services);
    }

    private static async Task RunTestRequests(ServiceCollection services)
    {
        // Arrange
        var provider = services.BuildServiceProvider();
        var query = new TestQuery1(2);
        var command = new TestResultCommand1(1);
        var unitCommand = new TestUnitCommand1();
        var mediator = new ServiceMediator(provider);

        // Act
        var queryResult = await mediator.SendQuery<TestQuery1, int>(query, CancellationToken.None);
        var commandResult = await mediator.SendCommand<TestResultCommand1, int>(command, CancellationToken.None);
        var unitCommandResult = await mediator.SendCommand<TestUnitCommand1>(unitCommand, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => ResultAssert.IsSuccess(queryResult),
            () => query.AssertHandled(queryResult),
            () => ResultAssert.IsSuccess(commandResult),
            () => command.AssertHandled(commandResult),
            () => ResultAssert.IsSuccess(unitCommandResult),
            () => unitCommand.AssertHandled());
    }
}
