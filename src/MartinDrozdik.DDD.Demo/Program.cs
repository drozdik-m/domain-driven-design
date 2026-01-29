using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Options;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Mediator.Pipelines.Integrators;
using MartinDrozdik.DDD.Mediator.Pipelines.Validations;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Logging;
using MartinDrozdik.DDD.Web.Mediator.Pipelines.Logging;
using MartinDrozdik.DDD.Web.Middlewares;
using MartinDrozdik.DDD.Web.Middlewares.Logging;
using MartinDrozdik.DDD.Web.OpenApi;
using MartinDrozdik.DDD.Web.Options;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// Add options
builder.Services.AddValidatedAppOptions<InvoiceOptions>();

// Add logging
builder.AddAppLogging();

// Add error handling
builder.Services.AddAppErrorHandling();

// Add DbContext with SQLite
builder.AddAppDbContext<InvoiceDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

builder.Services.AddControllers();
builder.Services.AddAppOpenApi();

builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();
    config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>(integration);
    config.WithCommand<CreateInvoiceDraftCommand, InvoiceId, CreateInvoiceDraftCommandHandler>(integration);
});

// --- APP ---
var app = builder.Build();

await app.EnsureCreatedDatabaseAsync<InvoiceDbContext>();

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
