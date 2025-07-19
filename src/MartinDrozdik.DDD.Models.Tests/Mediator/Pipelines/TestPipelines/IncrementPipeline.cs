using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestPipelines;

internal class IncrementPipeline : IPipelineBehavior<int, int>
{
    public async Task<int> HandleAsync(int input, PipelineNextDelegate<int> next, CancellationToken cancellationToken)
    {
        var result = await next(input + 1, cancellationToken);
        return result;
    }
}
