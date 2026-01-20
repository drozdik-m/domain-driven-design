using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

        // Add options
        //builder.Services.AddValidatedAppOptions<InvoiceOptions>();

        // Add logging
        builder.Logging.AddConsole();

        // Add error handling
        /*builder.Services.AddProblemDetails()
            .AddExceptionHandler<BusinessRuleValidationExceptionHandler>()
            .AddExceptionHandler<ValidationExceptionHandler>()
            .AddExceptionHandler<GlobalExceptionHandler>();*/

        // Add DbContext with SQLite
        /*builder.Services.AddDbContext<InvoiceDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            options.UseSqlite(connectionString);

            // Enable sensitive data logging in development
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });*/

        builder.Services.AddControllers();
        /*builder.Services.AddOpenApi(options =>
        {
            options.ParentDeclarationSchemaIds();
        });*/
        /*
        builder.Services.AddMediator(config =>
        {
            var pipelineBuilder = new PipelineAssistant();
            config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>(pipelineBuilder);
            config.WithCommand<CreateInvoiceDraftCommand, InvoiceId, CreateInvoiceDraftCommandHandler>(pipelineBuilder);
        });*/

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHttpLogging(options =>
            {
                options.LoggingFields = HttpLoggingFields.All;
                options.CombineLogs = true;
            });
        }

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

        app.UseAuthorization();

        app.MapControllers();

        app.UseStatusCodePages();

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
}
