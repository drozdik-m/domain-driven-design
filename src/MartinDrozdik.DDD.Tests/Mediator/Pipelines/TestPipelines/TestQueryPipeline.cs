using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestPipelines;

internal class TestQueryPipeline(string id) : IPipelineBehavior<TestPipelineQuery, int>
{
    public async Task<int> HandleAsync(TestPipelineQuery input, PipelineNextDelegate<int> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        input.AddCall(id);
        return result + 1;
    }
}
