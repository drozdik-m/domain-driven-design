using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines;

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
            () => result.IsSuccess(),
            () => query.AssertCallStack(),
            () => Assert.Equal(result.Value, query.Result));
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
            () => result.IsSuccess(),
            () => command.AssertCallStack(),
            () => Assert.Equal(result.Value, command.Result));
    }

    [Fact]
    public async Task Empty_unit_command_pipeline_does_nothing()
    {
        // Arrange
        var pipeline = EmptyPipeline<TestPipelineUnitCommand>.Instance;
        var command = new TestPipelineUnitCommand();

        // Act
        var result = await pipeline.HandleCommandAsync(
            command,
            new TestPipelineUnitCommandHandler(),
            CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => command.AssertCallStack());
    }
}
