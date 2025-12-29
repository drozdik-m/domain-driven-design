using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Queries;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Pipelines.TestRequests;

internal record TestPipelineQuery(int Result) : IQuery<int>
{
    private readonly List<string> _callStack = [];

    public void AddCall(string id)
    {
        _callStack.Add(id);
    }

    public void AssertCallStack(params string[] expected)
    {
        Assert.Equal(expected, _callStack);
    }
}

internal class TestPipelineQueryHandler : IQueryHandler<TestPipelineQuery, int>
{
    public Task<int> HandleAsync(TestPipelineQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(query.Result);
    }
}
