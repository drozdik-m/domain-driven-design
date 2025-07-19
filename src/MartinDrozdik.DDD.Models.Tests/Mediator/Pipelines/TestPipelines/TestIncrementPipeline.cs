using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;

internal class TestIncrementPipeline(string id) : IPipelineBehavior<TestPipelineQuery, int>
{
    public async Task<Result<int, Error>> HandleAsync(TestPipelineQuery input, PipelineNextDelegate<int> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);
        input.AddCall(id);
        return result.Value + 1;
    }
}
