using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Middlewares.Exceptions;
using MartinDrozdik.DDD.Demo.Middlewares.OpenApi;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Demo.Requests.Pipelines;
using MartinDrozdik.DDD.Models.Mediator;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using MartinDrozdik.DDD.Models.Mediator.Pipelines;
using MartinDrozdik.DDD.Models.Mediator.Queries;
using MartinDrozdik.Hosting.Observability.Logging;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ICommand = MartinDrozdik.DDD.Models.Mediator.Commands.ICommand;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();

// Add error handling
builder.Services.AddProblemDetails()
    .AddExceptionHandler<BusinessRuleValidationExceptionHandler>()
    .AddExceptionHandler<ValidationExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

// Add DbContext with SQLite
builder.Services.AddDbContext<InvoiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlite(connectionString);

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.ParentDeclarationSchemaIds();
});

builder.Services.AddMediator(config =>
{
    var pipelineBuilder = new PipelineAssistant();
    config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>(pipelineBuilder);
    config.WithCommand<CreateInvoiceDraftCommand, InvoiceId, CreateInvoiceDraftCommandHandler>(pipelineBuilder);
});

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
try
{
    await using var context = app.Services.CreateAsyncScope();
    using var dbContext = context.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An error occurred creating the DB.");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
}
app.UseMiddleware<RequestResponseLoggingMiddleware>();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStatusCodePages();

await app.RunAsync();


class PipelineAssistant : ServiceMediatorConfig.IPipelineAssistant
{
    /// <inheritdoc />
    public IServiceCollection RegisterQueryPipeline<TQuery, TOutput>(IServiceCollection services) where TQuery : IQuery<TOutput>
    {
        services.AddScoped<LoggingPipeline<TQuery, TOutput>>();
        return services;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TQuery, TOutput> BuildQueryPipeline<TQuery, TOutput>() where TQuery : IQuery<TOutput>
    {
        return new ServicePipelineBuilder<TQuery, TOutput>()
            .Add<LoggingPipeline<TQuery, TOutput>>();
    }

    /// <inheritdoc />
    public IServiceCollection RegisterCommandPipeline<TCommand, TOutput>(IServiceCollection services) where TCommand : ICommand<TOutput>
    {
        services.AddScoped<LoggingPipeline<TCommand, TOutput>>();
        return services;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand, TOutput> BuildCommandPipeline<TCommand, TOutput>() where TCommand : ICommand<TOutput>
    {
        return new ServicePipelineBuilder<TCommand, TOutput>()
            .Add<LoggingPipeline<TCommand, TOutput>>();
    }

    /// <inheritdoc />
    public IServiceCollection RegisterUnitCommandPipeline<TCommand>(IServiceCollection services) where TCommand : ICommand
    {
        services.AddScoped<LoggingPipeline<TCommand>>();
        return services;
    }

    /// <inheritdoc />
    public ServicePipelineBuilder<TCommand> BuildUnitCommandPipeline<TCommand>() where TCommand : ICommand
    {
        return new ServicePipelineBuilder<TCommand>()
            .Add<LoggingPipeline<TCommand>>();
    }
}
