using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Options;
using MartinDrozdik.DDD.Demo.RecurringTasks;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Mediator.Pipelines.Integrators;
using MartinDrozdik.DDD.Mediator.Pipelines.Validations;
using MartinDrozdik.DDD.Web;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Environments;
using MartinDrozdik.DDD.Web.Mediator.Pipelines.Logging;
using MartinDrozdik.DDD.Options;
using MartinDrozdik.DDD.Web.Proxy;
using MartinDrozdik.DDD.Web.RecurringTasks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

var options = MartinDrozdik.DDD.Web.WebApplicationOptions.Default with
{
    UseStaticFilePathProvider = false,
};
builder.AddAppServices(options);

// Options
builder.Services.AddValidatedAppOptions<InvoiceOptions>();

// Add DbContext with SQLite
builder.AddAppDbContext<InvoiceDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

// A background job on a schedule, also triggerable on demand from a controller
builder.AddRecurringTask<InvoiceVolumeReportTask>(taskOptions =>
{
    taskOptions.InitialDelay = TimeSpan.FromSeconds(30);
    taskOptions.Period = TimeSpan.FromMinutes(1);
});

builder.Services.AddControllers();

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

app.IsBehindProxy(); // well not actually, but this is how you would configure it if you were
app.UseAppMiddlewares(options);

if (app.Environment.IsDevelopment() || app.Environment.IsTesting())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseStatusCodePages();

await app.RunAsync();
