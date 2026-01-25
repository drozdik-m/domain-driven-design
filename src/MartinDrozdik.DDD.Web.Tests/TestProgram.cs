using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Logging;
using MartinDrozdik.DDD.Web.Middlewares;
using MartinDrozdik.DDD.Web.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Tests;

public class TestProgram
{
    /// <summary>
    /// Testing program main entry point.
    /// </summary>
    /// <param name="args">Application arguments.</param>
    /// <remarks>Must be internal to not be considered entry point.</remarks>
    internal static void Main(string[] args)
    {
        // --- BUILDER ---
        var builder = WebApplication.CreateBuilder(args);
        builder.AddAppLogging();
        builder.Services.AddAppErrorHandling();
        builder.Services.AddAppOpenApi();

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

        // Ensure DB is created
        /*try
        {
            await using var context = app.Services.CreateAsyncScope();
            using var dbContext = context.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred creating the DB.");
            throw;
        }*/

        //app.UseMiddleware<RequestResponseLoggingMiddleware>();

        // TODO make tests app.MapOpenApi();

        app.MapGet("/", () => "Hello World!");

        app.Run();
        /*
        class PipelineAssistant : ServiceMediatorConfig.IPipelineAssistant
        {
            /// <inheritdoc />
            public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services) where TQuery : IQuery<TOutput>
            {
                services.AddScoped<LoggingPipeline<TQuery, TOutput>>();
                services.AddScoped<ValidationPipeline<TQuery, TOutput>>();
                return services;
            }

            /// <inheritdoc />
            public ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>() where TQuery : IQuery<TOutput>
            {
                return new ServicePipelineBuilder<TQuery, TOutput>()
                    .Add<LoggingPipeline<TQuery, TOutput>>()
                    .Add<ValidationPipeline<TQuery, TOutput>>();
            }

            /// <inheritdoc />
            public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services) where TCommand : ICommand<TOutput>
            {
                services.AddScoped<LoggingPipeline<TCommand, TOutput>>();
                services.AddScoped<ValidationPipeline<TCommand, TOutput>>();
                return services;
            }

            /// <inheritdoc />
            public ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>() where TCommand : ICommand<TOutput>
            {
                return new ServicePipelineBuilder<TCommand, TOutput>()
                    .Add<LoggingPipeline<TCommand, TOutput>>()
                    .Add<ValidationPipeline<TCommand, TOutput>>();
            }

            /// <inheritdoc />
            public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services) where TCommand : ICommand
            {
                services.AddScoped<LoggingPipeline<TCommand>>();
                services.AddScoped<ValidationPipeline<TCommand>>();
                return services;
            }

            /// <inheritdoc />
            public ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>() where TCommand : ICommand
            {
                return new ServicePipelineBuilder<TCommand>()
                    .Add<LoggingPipeline<TCommand>>()
                    .Add<ValidationPipeline<TCommand>>();
            }
        }
        */
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
    }
}
