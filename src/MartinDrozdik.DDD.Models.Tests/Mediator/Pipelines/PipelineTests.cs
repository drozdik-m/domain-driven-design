using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines;

public class PipelineTests
{
    [Fact]
    public async Task Success_query_pipeline_passes()
    {
        // Arrange
        var pipelines = new List<IPipelineBehavior<TestPipelineQuery, int>>
        {
            new TestQueryPipeline("id1"),
            new TestQueryPipeline("id2"),
        };
        var pipeline = new Pipeline<TestPipelineQuery, int>(pipelines);
        var query = new TestPipelineQuery(1);
        var handler = new TestPipelineQueryHandler();

        // Act
        var result = await pipeline.HandleQueryAsync(query, handler, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => query.AssertCallStack("id1", "id2"),
            () => Assert.Equal(result.Value, query.Result + 2));
    }

    [Fact]
    public async Task Success_command_pipeline_passes()
    {
        // Arrange
        var pipelines = new List<IPipelineBehavior<TestPipelineCommand, int>>
        {
            new TestCommandPipeline("id1"),
            new TestCommandPipeline("id2"),
        };
        var pipeline = new Pipeline<TestPipelineCommand, int>(pipelines);
        var command = new TestPipelineCommand(1);
        var handler = new TestPipelineCommandHandler();

        // Act
        var result = await pipeline.HandleCommandAsync(command, handler, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => command.AssertCallStack("id1", "id2"),
            () => Assert.Equal(result.Value, command.Result + 2));
    }

    [Fact]
    public async Task Success_unit_command_pipeline_passes()
    {
        // Arrange
        var pipelines = new List<IPipelineBehavior<TestPipelineUnitCommand>>
        {
            new TestUnitCommandPipeline("id1"),
            new TestUnitCommandPipeline("id2"),
        };
        var pipeline = new Pipeline<TestPipelineUnitCommand>(pipelines);
        var command = new TestPipelineUnitCommand();
        var handler = new TestPipelineUnitCommandHandler();

        // Act
        var result = await pipeline.HandleCommandAsync(command, handler, CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => command.AssertCallStack("id1", "id2"));
    }
}
