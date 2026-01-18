using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines;

public class EmptyPipelineTests
{
    [Fact]
    public async Task Empty_query_pipeline_does_nothing()
    {
        // Arrange
        var pipeline = EmptyPipeline<TestPipelineQuery, int>.Instance;
        var query = new TestPipelineQuery(1);

        // Act
        var result = await pipeline.HandleQueryAsync(
            query,
            new TestPipelineQueryHandler(),
            CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => query.AssertCallStack(),
            () => Assert.Equal(result, query.Result));
    }

    [Fact]
    public async Task Empty_command_pipeline_does_nothing()
    {
        // Arrange
        var pipeline = EmptyPipeline<TestPipelineCommand, int>.Instance;
        var command = new TestPipelineCommand(1);

        // Act
        var result = await pipeline.HandleCommandAsync(
            command,
            new TestPipelineCommandHandler(),
            CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => command.AssertCallStack(),
            () => Assert.Equal(result, command.Result));
    }

    [Fact]
    public async Task Empty_unit_command_pipeline_does_nothing()
    {
        // Arrange
        var pipeline = EmptyPipeline<TestPipelineUnitCommand>.Instance;
        var command = new TestPipelineUnitCommand();

        // Act
        await pipeline.HandleCommandAsync(
            command,
            new TestPipelineUnitCommandHandler(),
            CancellationToken.None);

        // Assert
        command.AssertCallStack();
    }
}
