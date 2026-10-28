using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Blobs.Outbox;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Interceptors;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices();

// Add DbContext with SQLite
builder.AddAppDbContext<TestDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor<TestDbContext>>());
});

// A second bounded context, in a database of its own
builder.AddAppDbContext<SecondTestDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(SecondTestDbContext.GetConnectionString(options.ConnectionString));
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor<SecondTestDbContext>>());
});

// A recurring task scheduled far enough away that it only runs when a test triggers it
builder.Services.AddSingleton<TestRecurringTaskRuns>();
builder.AddRecurringTask<TestRecurringTask>(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

// MessageOutbox - one per context
builder.Services.AddSingleton<TestOutboxHandlerState>();
builder.AddOutbox<TestDbContext>(config => config
    .WithMessage<TestOutboxMessage, TestOutboxMessageHandler>()
    .WithMessage<OtherTestOutboxMessage, OtherTestOutboxMessageHandler>()
    .WithBlobs());

builder.AddOutbox<SecondTestDbContext>(
    options => options.SerializerOptions = SecondTestDbContext.OutboxSerializerOptions,
    config => config
        .WithMessage<TestOutboxMessage, TestOutboxMessageHandler>() // Shared with the first outbox, as a shared infrastructure project would
        .WithMessage<SecondContextOutboxMessage, SecondContextOutboxMessageHandler>());

// Blob storage, in folders of this instance's own so parallel test apps cannot see each other
var blobRoot = Path.Combine(Path.GetTempPath(), $"{Guid.CreateVersion7():N}_test_blobs");
builder.AddBlobs<TestDbContext>(blobs => blobs
    .WithContainer(TestBlobContainers.Invoices)
    .WithContainer(TestBlobContainers.Avatars, options => options.MaxSize = TestBlobContainers.AvatarMaxSize));
builder.AddFileBlobStore(files => files
    .WithContainer(TestBlobContainers.Invoices, Path.Combine(blobRoot, TestBlobContainers.Invoices))
    .WithContainer(TestBlobContainers.Avatars, Path.Combine(blobRoot, TestBlobContainers.Avatars)));

// Scheduled far enough away that it only runs when a test asks for it
builder.AddBlobSweepRecurringTask(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

builder.AddOutboxDispatchRecurringTask<TestDbContext>(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

builder.AddOutboxDispatchRecurringTask<SecondTestDbContext>(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

builder.AddOutboxRetention<TestDbContext>(options => options.Retention = TimeSpan.FromDays(7));
builder.AddOutboxRetentionRecurringTask<TestDbContext>(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

// --- APP ---
var app = builder.Build();

await app.EnsureCreatedDatabaseAsync<TestDbContext>();
await app.EnsureCreatedDatabaseAsync<SecondTestDbContext>();

app.UseAppMiddlewares();

app.MapOpenApi("/openapi/doc.json");
app.MapOpenApi("/openapi/doc.yaml");

app.MapGet("/", () => "Hello World!");

// Endpoints throwing business exceptions, used to verify how the middleware pipeline logs them
app.MapGet("/throw/not-found", string () => throw new BusinessNotFoundException("Nothing here"));
app.MapGet("/throw/unhandled", string () => throw new InvalidOperationException("Boom"));

await app.RunAsync();
