using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;

internal class TestCommandPipeline(string id) : IPipelineBehavior<TestPipelineCommand, int>
{
    public async Task<int> HandleAsync(TestPipelineCommand input, PipelineNextDelegate<int> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        input.AddCall(id);
        return result + 1;
    }
}
