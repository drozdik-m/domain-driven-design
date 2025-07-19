using MartinDrozdik.DDD.Models.Mediator;
using MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines;

public class PipelineTests
{
    [Fact]
    public async Task Success_pipeline_passes()
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
            () => result1.IsSuccess(),
            () => result2.IsSuccess(),
            () => command1.AssertHandled(),
            () => command2.AssertHandled());
    }

    private static ServiceMediator CreateMediatorWithHandlers()
    {
        var services = new ServiceCollection();
        services.AddTestRequests();
        var provider = services.BuildServiceProvider();
        return new ServiceMediator(provider);
    }
}
