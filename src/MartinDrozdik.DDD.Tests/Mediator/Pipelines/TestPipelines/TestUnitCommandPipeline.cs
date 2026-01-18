using MartinDrozdik.DDD.Mediator.Pipelines;
using MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Tests.Mediator.Pipelines.TestPipelines;

internal class TestUnitCommandPipeline(string id) : IPipelineBehavior<TestPipelineUnitCommand>
{
    public async Task HandleAsync(TestPipelineUnitCommand input, PipelineNextDelegate next, CancellationToken cancellationToken)
    {
        await next(cancellationToken);
        input.AddCall(id);
    }
}
