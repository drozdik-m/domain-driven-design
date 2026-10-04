using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Demo.Options;
using MartinDrozdik.DDD.Demo.Outbox;
using MartinDrozdik.DDD.Demo.RecurringTasks;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Mediator;
using MartinDrozdik.DDD.Mediator.Pipelines.Integrators;
using MartinDrozdik.DDD.Mediator.Pipelines.Validations;
using MartinDrozdik.DDD.Web;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Outbox;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Interceptors;
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
builder.AddAppDbContext<InvoiceDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);

    // Optional: without it an enqueued message simply waits for the next poll
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor>());
});

// A background job on a schedule, also triggerable on demand from a controller
builder.AddRecurringTask<InvoiceVolumeReportTask>(taskOptions =>
{
    taskOptions.InitialDelay = TimeSpan.FromSeconds(30);
    taskOptions.Period = TimeSpan.FromMinutes(1);
});

// A transactional outbox over the same context
// - AddOutbox is the engine
// - AddOutboxDispatchRecurringTask is the schedule that drives it - drop to drive IOutboxProcessor from Quartz.NET or anything else instead.
builder.AddOutbox<InvoiceDbContext>(
    outboxOptions => outboxOptions.Retention = TimeSpan.FromDays(7),
    config => config
        .WithMessage<InvoiceDraftedMessage, InvoiceDraftedMessageHandler>()
        .WithBlobs()); // Blob deletion rides on the same outbox

// Blob storage over the same context
builder.AddBlobs<InvoiceDbContext>(blobs => blobs
    .WithContainer(BlobContainers.InvoiceScans, containerOptions =>
    {
        containerOptions.MaxSize = 20 * 1024 * 1024;
        containerOptions.AllowedExtensions = new HashSet<string>(StringComparer.Ordinal) { "pdf", "png", "jpg", "jpeg" };
    }));

builder.AddFileBlobStore(files => files
    .WithContainer(BlobContainers.InvoiceScans, Path.Combine(builder.Environment.ContentRootPath, "blobs", BlobContainers.InvoiceScans.Name)));

builder.AddBlobSweepRecurringTask(taskOptions =>
{
    taskOptions.InitialDelay = TimeSpan.FromMinutes(1);
    taskOptions.Period = TimeSpan.FromHours(1);
});

builder.AddOutboxDispatchRecurringTask(taskOptions =>
{
    taskOptions.InitialDelay = TimeSpan.FromSeconds(10);
    taskOptions.Period = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers();

builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();
    config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>(integration);
    config.WithCommand<CreateInvoiceDraftCommand, InvoiceId, CreateInvoiceDraftCommandHandler>(integration);
    config.WithCommand<AttachInvoiceScanCommand, BlobId, AttachInvoiceScanCommandHandler>(integration);
    config.WithCommand<RemoveInvoiceScanCommand, RemoveInvoiceScanCommandHandler>(integration);
    config.WithQuery<GetInvoiceScanQuery, BlobContent, GetInvoiceScanQueryHandler>(integration);
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
