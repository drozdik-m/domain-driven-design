using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;

internal class TestUnitCommandPipeline(string id) : IPipelineBehavior<TestPipelineUnitCommand>
{
    public async Task<UnitResult<Error>> HandleAsync(TestPipelineUnitCommand input, PipelineNextDelegate next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        input.AddCall(id);
        return result;
    }
}
