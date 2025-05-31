using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.Requests;

internal record TestResultCommand2(int Result) : ICommand<int>
{
    public void AssertHandled(Result<int, Error> result)
    {
        ResultAssert.IsSuccess(result);
        Assert.Equal(Result, result.Value);
    }
}

internal class TestResultCommand2Handler : ICommandHandler<TestResultCommand2, int>
{
    public Task<Result<int, Error>> HandleAsync(TestResultCommand2 command, CancellationToken cancellationToken)
    {
        var result = Result.Success<int, Error>(command.Result);
        return Task.FromResult(result);
    }
}
