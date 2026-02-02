using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Health;
using MartinDrozdik.DDD.Web.Logging;
using MartinDrozdik.DDD.Web.Middlewares;
using MartinDrozdik.DDD.Web.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestProgram
{
    /// <summary>
    /// Testing program main entry point.
    /// </summary>
    /// <param name="args">Application arguments.</param>
    /// <remarks>Must be internal to not be considered entry point.</remarks>
    /// <returns><see cref="Task"/>.</returns>
    internal static async Task Main(string[] args)
    {
        // --- BUILDER ---
        var builder = WebApplication.CreateBuilder(args);
        builder.AddAppLogging();
        builder.Services.AddAppErrorHandling();
        builder.Services.AddAppOpenApi();
        builder.AddAppHealthChecks();

        // Add DbContext with SQLite
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_db.db");
        var connectionString = $"Data Source={dbPath}";
        builder.Configuration[$"{DatabaseOptions.Section}:{nameof(DatabaseOptions.ConnectionString)}"] = connectionString;
        builder.AddAppDbContext<TestDbContext>((options, dbBuilder) =>
        {
            dbBuilder.UseSqlite(options.ConnectionString);
        });

        /*
        builder.Services.AddMediator(config =>
        {
            var pipelineBuilder = new PipelineAssistant();
            config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>(pipelineBuilder);
            config.WithCommand<CreateInvoiceDraftCommand, InvoiceId, CreateInvoiceDraftCommandHandler>(pipelineBuilder);
        });*/

        // --- APP ---
        var app = builder.Build();

        app.UseExceptionHandler();
        app.MapAppHealthChecks();

        await app.EnsureCreatedDatabaseAsync<TestDbContext>();

        //app.UseMiddleware<RequestResponseLoggingMiddleware>();

        app.MapOpenApi("/openapi/doc.json");
        app.MapErrorEndpoints();

        app.MapGet("/", () => "Hello World!");

        await app.RunAsync();
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
    }
}
