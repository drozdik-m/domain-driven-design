using MartinDrozdik.DDD.Models.Mediator;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines;

public class PipelineTests
{
    [Fact]
    public async Task Success_pipeline_passes()
    {
        // Arrange
        var pipelines = new List<IPipelineBehavior<TestQuery1, int>>
        {
            new TestIncrementPipeline(),
            new TestIncrementPipeline(),
        };
        var pipeline = new Pipeline<TestQuery1, int>(pipelines);
        var query = new TestQuery1(1);
        var handler = new TestQuery1Handler();

        // Act
        var result = await pipeline.HandleAsync(
            query,
            async (cancellationToken) => await handler.HandleAsync(query, cancellationToken),
            CancellationToken.None);

        // Assert
        Assert.Multiple(
            () => result.IsSuccess(),
            () => query.AssertHandled(result),
            () => Assert.Equal(result.Value, query.Result + 2));
    }
}
