using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// Extensions for <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Ensure that database of a context is deleted.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="app">The <see cref="WebApplication"/> where the <see cref="DbContext"/> is located.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task EnsureDeletedDatabaseAsync<T>(this WebApplication app)
        where T : DbContext
    {
        try
        {
            if (app.Logger.IsEnabled(LogLevel.Information))
            {
                app.Logger.LogInformation("Ensuring database for {DbContext} is deleted.", typeof(T).Name);
            }

            await app.ExecuteContextOperationAsync<T>(e => e.Database.EnsureDeletedAsync());
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception e)
        {
            app.Logger.LogError(e, "An error occurred while deleting database for {DbContext}.", typeof(T).Name);
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    /// <summary>
    /// Ensure that database of a context is created.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="app">The <see cref="WebApplication"/> where the <see cref="DbContext"/> is located.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task EnsureCreatedDatabaseAsync<T>(this WebApplication app)
        where T : DbContext
    {
        try
        {
            if (app.Logger.IsEnabled(LogLevel.Information))
            {
                app.Logger.LogInformation("Ensuring database for {DbContext} is created.", typeof(T).Name);
            }

            await app.ExecuteContextOperationAsync<T>(e => e.Database.EnsureCreatedAsync());
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception e)
        {
            app.Logger.LogError(e, "An error occurred while creating database for {DbContext}.", typeof(T).Name);
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    /// <summary>
    /// Ensure that database of a context is migrated.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="app">The <see cref="WebApplication"/> where the <see cref="DbContext"/> is located.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task EnsureMigratedDatabaseAsync<T>(this WebApplication app)
        where T : DbContext
    {
        try
        {
            if (app.Logger.IsEnabled(LogLevel.Information))
            {
                app.Logger.LogInformation("Ensuring database for {DbContext} is migrated.", typeof(T).Name);
            }

            await app.ExecuteContextOperationAsync<T>(e => e.Database.MigrateAsync());
        }
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        catch (Exception e)
        {
            app.Logger.LogError(e, "An error occurred while migrating database for {DbContext}.", typeof(T).Name);
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    /// <summary>
    /// Execute operation with a <see cref="DbContext"/> from the service provider.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="app">The <see cref="WebApplication"/> where the <see cref="DbContext"/> is located.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns><see cref="Task"/>.</returns>
    private static async Task ExecuteContextOperationAsync<T>(this WebApplication app, Func<T, Task> operation)
        where T : DbContext
    {
        await using var context = app.Services.CreateAsyncScope();
        using var dbContext = context.ServiceProvider.GetService<T>()
            ?? throw new InvalidOperationException($"DbContext of type {typeof(T).Name} is not registered in the service provider.");
        await operation(dbContext);
    }
}
