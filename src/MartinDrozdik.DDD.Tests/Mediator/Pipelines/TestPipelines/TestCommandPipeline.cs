using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestPipelines;

internal class TestCommandPipeline(string id) : IPipelineBehavior<TestPipelineCommand, int>
{
    public async Task<int> HandleAsync(TestPipelineCommand input, PipelineNextDelegate<int> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        input.AddCall(id);
        return result + 1;
    }
}
