using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Models.Tests.Mediator.TestRequests;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestRequests(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<TestUnitCommand1>, TestUnitCommand1Handler>();
        services.AddTransient<ICommandHandler<TestUnitCommand2>, TestUnitCommand2Handler>();
        services.AddTransient<ICommandHandler<TestResultCommand1, int>, TestResultCommand1Handler>();
        services.AddTransient<ICommandHandler<TestResultCommand2, int>, TestResultCommand2Handler>();
        services.AddTransient<IQueryHandler<TestQuery1, int>, TestQuery1Handler>();
        services.AddTransient<IQueryHandler<TestQuery2, int>, TestQuery2Handler>();
        return services;
    }
}
