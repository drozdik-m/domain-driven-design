using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines;

public class PipelineTests
{
    [Fact]
    public async Task Success_pipeline_passes()
    {
        // Arrange
        var pipelines = new List<IPipelineBehavior<TestPipelineQuery, int>>
        {
            new TestIncrementPipeline("id1"),
            new TestIncrementPipeline("id2"),
        };
        var pipeline = new Pipeline<TestPipelineQuery, int>(pipelines);
        var query = new TestPipelineQuery(1);
        var handler = new TestPipelineQueryHandler();

        // Act
        var result = await pipeline.HandleAsync(
            query,
            async (cancellationToken) => await handler.HandleAsync(query, cancellationToken),
            CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => query.AssertCallStack("id1", "id2"),
            () => Assert.Equal(result.Value, query.Result + 2));
    }
}
