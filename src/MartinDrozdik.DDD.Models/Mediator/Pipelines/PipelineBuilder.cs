using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MartinDrozdik.DDD.Models.Mediator.Exceptions;

namespace MartinDrozdik.DDD.Models.Mediator.Pipelines;
/*
public class PipelineBuilder<TInput, TOutput>
{
    private IPipelineBehavior<TInput, TOutput> _current = EmptyPipeline<TInput, TOutput>.Instance;

    public PipelineBuilder Add(Func<PipelineContext, Task> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Returns the entry point of the pipeline.
    /// </summary>
    public IPipelineBehavior<TInput, TOutput> Build()
    {
        return _current;
    }
}
*/
