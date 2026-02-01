using FluentValidation;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web.Middlewares.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Web.Middlewares;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds problem details and exception handlers to the service collection.
    /// Dont forget to add <see cref="ExceptionHandlerExtensions.UseExceptionHandler(IApplicationBuilder)"/>.
    /// Handles:
    /// <list type="bullet">
    /// <item><see cref="BusinessRuleValidationException"/></item>
    /// <item><see cref="BusinessRuleException"/></item>
    /// <item><see cref="ValidationException"/></item>
    /// <item><see cref="Exception"/></item>
    /// </list>
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to extend.</param>
    /// <returns>Updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppErrorHandling(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddProblemDetails()
            .AddExceptionHandler<BusinessRuleValidationExceptionHandler>()
            .AddExceptionHandler<ValidationExceptionHandler>()
            .AddExceptionHandler<GlobalExceptionHandler>();
    }
}
