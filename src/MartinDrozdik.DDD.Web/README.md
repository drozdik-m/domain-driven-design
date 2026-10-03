# DDD for ASP.NET Core - Web Plumbing That Doesn't Suck

[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD.Web?style=flat-square&logo=nuget&label=MartinDrozdik.DDD.Web)](https://www.nuget.org/packages/MartinDrozdik.DDD.Web)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD.Web?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/MartinDrozdik.DDD.Web)
[![Build & Test](https://img.shields.io/github/actions/workflow/status/drozdik-m/domain-driven-design/main.yml?branch=main&style=flat-square&logo=github&label=actions)](https://github.com/drozdik-m/domain-driven-design/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/drozdik-m/domain-driven-design?style=flat-square)](https://github.com/drozdik-m/domain-driven-design/blob/main/LICENSE)

Opinionated web infrastructure for .NET based on [MartinDrozdik.DDD](../MartinDrozdik.DDD) and [MartinDrozdik.DDD.Options](../MartinDrozdik.DDD.Options). Includes error handling, logging, telemetry, health checks, and other setup you'll need anyway. Check the [demo](../MartinDrozdik.DDD.Demo).

## Installation

```bash
dotnet add package MartinDrozdik.DDD.Web
```

Also check this repos' [DDD Claude Code plugin](../../claude/README.md) for better AI code generation.

## Philosophy

**Same as the [core DDD library](../MartinDrozdik.DDD).**

This package provides **basic scaffolding for ASP.NET apps** while staying out of your way when you need to do your thing. **Everything is optional**, but it's tested together.

> iT jUsT wOrKs

## Quick Start

**Bare minimum setup** - one liner to get logging, error handling, OpenAPI, health checks, telemetry, and HTTP resilience:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices(); // the magic one liner

// ...

var app = builder.Build();
app.UseAppMiddlewares(); // the other magic one liner

// ...

await app.RunAsync();
```

Done. You've got a production-ready baseline. Now go build your actual features.

You want to use **Aspire**? We got you fam! The OTEL works with Aspire out of the box.

## All-in-One Setup

The `AddAppServices()` extension registers:

- **Logging** - Structured logging that actually helps you debug but doesn't leak sensitive info in production
- **Error Handling** - Converts your [DDD exceptions](../MartinDrozdik.DDD/Exceptions) to proper HTTP *RFC7807* error responses
- **OpenAPI** - Auto-generated API docs (because manually writing swagger is hell)
- **Health Checks** - basic `/health`, `/health/live` and `/health/ready` endpoints
- **OpenTelemetry** - Metrics, traces, and logs (exports to OTLP when configured via [OTEL environment variables](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/))
- **HTTP Resilience** - Default policies for HTTP clients
- **Static file path provider** - Because you probably need to serve some static files at some point

Don't want all of it? Use the individual extensions instead. I won't judge.

Or just turn them off via settings:

```csharp
var options = MartinDrozdik.DDD.Web.WebApplicationOptions.Default with
{
    UseStaticFilePathProvider = false,
};
builder.AddAppServices(options);
```

## Modules

What goodies do you want to use? Just call the appropriate extension method:

### Options

In its own package - see [MartinDrozdik.DDD.Options](../MartinDrozdik.DDD.Options). This package adds `IWebHostBuilder.SetOption<TOptions>()` on top of it.

### EEE (Ezy Error Ehndling)

Automatic conversion of DDD exceptions to HTTP responses:

- `BusinessRuleValidationException` → 400 Bad Request
- `ValidationException` (FluentValidation) → 400 Bad Request
- `BusinessNotFoundException` → 404 Not Found
- `BusinessRuleException` → 500 Internal Server Error, via the catch-all handler rather than a dedicated one
- Anything else → 500 Internal Server Error

In **development, you get detailed info** — stack traces and exception details.

Note that the catch-all handler puts the raw `exception.Message` in the response in every environment, so keep anything sensitive out of the
messages of exceptions you expect to escape a handler.

The middleware handles it:

```csharp
builder.Services.AddAppErrorHandling();
//...
app.UseExceptionHandler();
```

Your domain layer throws exceptions. The middleware translates them. You don't touch HTTP in your business logic.

### Database Setup

There is 99 % chance you are using EF core with relational database.

We got you, just use the extension method:

```json
// appsettings.json
{
  "App": {
    "Database": {
      "ConnectionString": "Data Source=app.db"
    }
  }
}
```

```csharp
// With DatabaseOptions from config:
builder.AddAppDbContext<YourDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
    // Use SQL Server, PostgreSQL, MySQL, whatever you like.
});

// Or manual configuration:
builder.AddAppDbContext<YourDbContext>(dbBuilder =>
{
    dbBuilder.UseSqlServer(connectionString);
});
```

- In development: sensitive data logging and detailed errors.
- In production: none of that.

Ensure your database exists:

```csharp
await app.EnsureCreatedDatabaseAsync<YourDbContext>();
```

Or for you migration folks:

```csharp
await app.EnsureMigratedDatabaseAsync<YourDbContext>();
```

And `EnsureDeletedDatabaseAsync<YourDbContext>()` when you want it gone.

### Health Checks

The most basic liveness and readiness probes. Because Kubernetes will ask:

```csharp
builder.AddAppHealthChecks();

// Or add custom checks:
builder.AddAppHealthChecks(checks =>
{
    // Add more checks as needed
});

app.MapAppHealthChecks(); // Registers /health/live and /health/ready
```

Timeouts are configured. Todd says "it just works".

### Mediator

Automatic request/response logging for your CQRS handlers:

```csharp
builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();
    
    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(integration);
});
```

Every command and query gets logged with info.

### Telemetry (OpenTelemetry)

Traces, metrics, and logs for ASP.NET Core and HTTP clients:

```csharp
builder.AddAppOpenTelemetry();
```

Configure export via environment variables:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
OTEL_SERVICE_NAME=your-service
OTEL_SERVICE_VERSION=1.0.0
```

Check the [docs with full list of OTEL environment variables](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/).

Health check requests are filtered out of traces because who cares about those.

### DDD Context / EF mapping

Expanded `DbContext` child called `DddDbContext` with some extra features tailored for DDD apps, such as:

- **OnAggregatesSave** – changed entries whose type is an `IAggregateRoot<T>`
- **OnDomainEntitiesSave** – changed entries whose type is an `IDomainEntity<T>`
- **OnObjectsSave** – every changed entry, whatever it is

Coming with useful mapping extensions. Example:

```csharp
public class InvoiceDbContext(/*...*/) : DddDbContext(options)
{
    private const string CreatedAtPropertyName = "CreatedAt";

    // ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        // Register audit shadow properties
        foreach (var entityType in modelBuilder.Model.GetAggregateRoots())
        {
            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(CreatedAtPropertyName);
            // ...
        }
    }

    protected override void OnAggregatesSave(IEnumerable<EntityEntry> entityEntries)
    {
        base.OnAggregatesSave(entityEntries);
        var now = timeProvider.GetUtcNow();
        foreach (var entry in entityEntries.Where(e => e.State == EntityState.Added))
        {
            entry.Property(CreatedAtPropertyName).CurrentValue = now;
        }
    }
}
```

### Reverse Proxy Support

One liner for handling proxied requests (nginx, YARP etc.):

```csharp
app.IsBehindProxy();
```

Handles `X-Forwarded-*` headers.

### HTTP Client Resilience

```csharp
builder.Services.AddHttpClientResilience();
```

Because the network is unreliable and you know it.

### Static File Path Provider

How much time did you spend solving an issue solved by clearing browser cache? *Yeah, me too.* This path modification with query string versioning is the most basic solution to this problem.

```csharp
IStaticFilePathProvider provider; // inject it where you need it
provider.PathTo("file.js");
```

Returns a version depending on the environment:

- **Development**: returns `"file.js?version={unix-timestamp}"` to bust cache on every request.
- **Production**: returns `"file.js?version=1.2.3"` depending on your appsettings. Bust cache when you deploy a new version, but not on every request.

```json
// appsettings.json
{
  "App": {
    "StaticFileVersioning": {
        "Version": "1.2.3"
    }
  }
}
```

### Recurring Background Tasks

**Every app ends up needing that one job on a loop.** Cleanup, reindexing, sending the queued mail. And every hand-rolled version gets the same four things wrong: it dies on the first unhandled exception, it grabs a `DbContext` from the root scope, it uses `Task.Delay` so you can't test it, and there's no way to say *"actually, run it now"*.

*Write the job. Skip the plumbing.*

```csharp
public class CleanupTask(InvoiceDbContext context, ILogger<CleanupTask> logger) : IRecurringTask
{
    // Resolved from a fresh DI scope every iteration, so scoped services just work
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var removed = await context.Drafts.Where(d => d.IsAbandoned).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Swept up {Removed} abandoned drafts.", removed);
    }
}

// Register it, schedule and all:
builder.AddRecurringTask<CleanupTask>(options =>
{
    options.InitialDelay = TimeSpan.FromSeconds(30);
    options.Period = TimeSpan.FromMinutes(10);
    options.Timeout = TimeSpan.FromMinutes(5);
});
```

- **`InitialDelay`** – how long to wait after startup, so background work doesn't elbow its way into the startup burst.
- **`Period`** – the gap between iterations, measured from when the previous one **finished**, not when it started. Iterations never overlap and a slow run can never build a backlog.
- **`Timeout`** – optional. Cancels the iteration's token and logs a warning the moment it overruns. The loop continues on once the iteration returns, so iterations still never overlap.
- **`Enabled`** – decided at startup. `false` and the loop never even begins.

All options are validated.

A failing iteration is logged with its exception and the loop keeps going. **One bad run does not silently kill your job**.

The schedule lives in code and memory. **Light and simple on purpose**. For heavy-duty complex stuff with complex crons and distributed schedules, I would recommend:

- [Quartz.NET](https://www.quartz-scheduler.net/)
- [Hangfire](https://www.hangfire.io/)

#### Immediate Trigger

**Need it to run right now?** Inject the trigger and ask:

```csharp
public class InvoicesController(IRecurringTaskTrigger<CleanupTask> trigger) : ControllerBase
{
    [HttpPost("cleanup")]
    public IActionResult Cleanup()
    {
        trigger.Trigger(); // returns immediately, the loop wakes up and does the work
        return Accepted();
    }
}
```

Triggers are **coalesced** — hammer it a thousand times during one iteration and you get **one** extra run, not a thousand.

It never blocks and never throws, so it's safe to call from anywhere.

### Transactional Outbox

**An outbox message is enqueued atomically with the rest of the DDD updates.**  It is a **reliable, transactional queue** that writes or reverts together with the domain updates.

The outbox makes the side effect part of the same transaction: the message is written to a table in *your* database by *your* `SaveChangesAsync`. Both, or neither. A background loop delivers it afterwards and keeps retrying until it sticks or finally dies.

A message is a plain serializable record with a stable storage key:

```csharp
public sealed record InvoiceDraftedMessage(Guid InvoiceId, string InvoiceNumber, string Recipient) : IOutboxMessage
{
    public static OutboxMessageType MessageType => "invoice.drafted.v1";
}
```

Its handler does the actual work. No try/catch, no bookkeeping -> throw and it gets retried as much as you want:

```csharp
public class InvoiceDraftedMessageHandler(IEmailSender sender) : IOutboxMessageHandler<InvoiceDraftedMessage>
{
    // Resolved from a fresh DI scope per message, so scoped services just work
    public Task HandleAsync(InvoiceDraftedMessage message, CancellationToken cancellationToken)
        => sender.SendAsync(message.Recipient, $"Invoice {message.InvoiceNumber} is ready.", cancellationToken);
}
```

Map the table into the context that owns your aggregates — same database, same transaction, that's the whole point:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

    modelBuilder.AddOutbox(); // table "OutboxMessage";
    // modelBuilder.AddOutbox("Messages", "outbox") to move it
}
```

Register the engine, the message types, and the schedule that drives it:

```csharp
builder.AddOutbox<InvoiceDbContext>(
    outboxOptions => outboxOptions.Retention = TimeSpan.FromDays(7), // or null for the defaults
    config => config
        .WithMessage<InvoiceDraftedMessage, InvoiceDraftedMessageHandler>()
        .WithMessage<InvoicePaidMessage, InvoicePaidMessageHandler>());

// The message consumer invoking the dispatch loop
builder.AddOutboxDispatchRecurringTask(schedule =>
{
    schedule.InitialDelay = TimeSpan.FromSeconds(10);
    schedule.Period = TimeSpan.FromSeconds(30);
});
```

Enqueue wherever the **business** demands it:

```csharp
public class CreateInvoiceDraftCommandHandler(InvoiceDbContext context, IOutbox<InvoiceDbContext> outbox)
    : ICommandHandler<CreateInvoiceDraftCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(CreateInvoiceDraftCommand command, CancellationToken cancellationToken)
    {
        var invoice = Invoice.CreateDraft(issuer, recipient, invoiceNumber);
        await context.Invoices.AddAsync(invoice, cancellationToken);

        outbox.AddOnSave(new InvoiceDraftedMessage(invoice.Id.Key, invoiceNumber.ToString(), recipient.FullName));

        await context.SaveChangesAsync(cancellationToken); // both, or neither
        return invoice.Id;
    }
}
```

`AddOnSave()` only tracks the row. **It never saves.** You save it all along with your aggregate.

`IOutbox<TContext>` names the context it tracks in, so an outbox can't be paired with another context's save by mistake.

`IOutbox<TContext>`, `IOutboxMessage`, `IOutboxMessageHandler<T>` and `OutboxMessageType` contracts live in the core [MartinDrozdik.DDD](../MartinDrozdik.DDD) package, so a business layer can be implemented without further dependencies.

#### Scheduled messages - undo what a rollback leaves behind

`AddOnSave()` covers "do this once my transaction commits". `AddNowAsync()` with a later `AvailableAt` covers the "undo this unless my transaction commits":

```csharp
var settings = new OutboxMessageSettings {
    AvailableAt = now + TimeSpan.FromHours(1) // schedule for later with grace period 1h
}; 
var removal = await outbox.AddNowAsync(new DeleteFileMessage(path), settings, ct); // committed at once
await WriteFileAsync(path, ct); // a side effect
await outbox.RemoveOnSaveAsync(removal, ct); // tracked
await context.SaveChangesAsync(ct); // removes it with your changes
```

The `*NowAsync` messages are committed at once through a fresh instance of your context from a scope. Your transactions, explicit or ambient, are left exactly as they were. `*OnSaveAsync()` work in your commit.

Committing on its own connection may work differently on some databases. F.e. SQLite starts transactions as immediate , and an `AddNowAsync()` inside it then waits for its own transaction until it times out. Best to call `*NowAsync()` before beginning your transaction, or begin a deferred one (`connection.BeginTransaction(deferred: true)` with `Database.UseTransaction`) and call it before its first save.

Set you preferred options for the outbox, like batch size, retry delays, retention, and so on:

```csharp
builder.AddOutbox<InvoiceDbContext>(options => { /* ... */ }, config => { /* ... */ });
```

Use health check to monitor large backlogs or dead-lettered messages:

```csharp
builder.AddAppHealthChecks(checks => checks.AddOutboxHealthCheck<InvoiceDbContext>());
```

#### Versioning – end the key with `.v1`

The key is what maps a stored row back to a type. **Rename it and every row carrying the old key becomes undeliverable.** So don't rename it: bump it.

```csharp
// v1 — shipped, rows in the table
public sealed record InvoiceDraftedMessage(Guid InvoiceId, string InvoiceNumber) : IOutboxMessage
{
    public static OutboxMessageType MessageType => "invoice.drafted.v1";
}

// Breaking change (Recipient is now required) -> a new type under a new key
public sealed record InvoiceDraftedV2Message(Guid InvoiceId, string InvoiceNumber, string Recipient) : IOutboxMessage
{
    public static OutboxMessageType MessageType => "invoice.drafted.v2";
}

// Both registered until the v1 rows have drained, then drop v1
config.WithMessage<InvoiceDraftedV1Message, InvoiceDraftedV1MessageHandler>()
      .WithMessage<InvoiceDraftedV2Message, InvoiceDraftedV2MessageHandler>();
```

Adding an optional member? Keep the key — an old payload still deserializes. Anything a stored v1 payload can't satisfy? Bump it.

Start at `.v1` on day one, even when you're sure it'll never change. It will.

#### Deliver it now, not in 30 seconds (optional)

Attach `OutboxTaskTriggerInterceptor` to your context and wake up the dispatch loop immediately to avoid waiting for the next poll.

```csharp
builder.AddAppDbContext<InvoiceDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor>());
});
```

You can also inject `IRecurringTaskTrigger<OutboxDispatchRecurringTask>` and call `Trigger()` from anywhere. It's **coalesced**.

*Purely about responsiveness. Leave it out and the message simply waits for the next poll.*

#### Bring your own scheduler

`AddOutboxDispatchRecurringTask` is just a thin shell. Skip it and drive `IOutboxProcessor` from Quartz.NET, Hangfire, or a cron hitting an endpoint:

```csharp
public sealed class OutboxJob(IOutboxProcessor processor) : IJob
{
    public Task Execute(IJobExecutionContext context)
        => processor.ProcessPendingAsync(context.CancellationToken);
}
```

### Blob Storage

**Solved saving files for you. And they commit together!** An invoice scan, a profile picture, a generated export - content too big for a column, referred by an aggregate.

Solid, testable abstraction around local file storage that most applications actually need.

Three components:

| Layer | Type | Knows about |
|---|---|---|
| Store | `IBlobStore` | Files, in-memory testing, the byte stuff |
| Catalogue | the `Blob` table | the row, the metadata, the transaction |
| Facade | `IBlobStorage` | coordinates both, applies policies, main interface for you |

**Blob storage requires the outbox.** Every removal of content is an outbox message: after a delete commits, after a blob expires, and after an upload that never committed its row.

Map the catalogue in your `OnModelCreating`, next to the outbox:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    modelBuilder.AddOutbox();
    modelBuilder.AddBlobs();
}
```

Register the engine with its containers, pick a store, and register its messages into the outbox:

```csharp
builder.AddBlobs<InvoiceDbContext>(blobs => blobs
    .WithContainer("invoice-scans", containerOptions =>
    {
        containerOptions.MaxSize = 20 * 1024 * 1024;
        containerOptions.AllowedExtensions = new HashSet<string>(StringComparer.Ordinal) { "pdf", "png", "jpg" };
    })
    .WithContainer("avatars", containerOptions => containerOptions.MaxSize = 512 * 1024)
    .WithSweep(sweepOptions => sweepOptions.BatchSize = 500));   // optional

// Where content is kept is a separate decision - each container gets a folder of its own
builder.AddFileBlobStore(files => files
    .WithContainer("invoice-scans", Path.Combine(builder.Environment.ContentRootPath, "blobs", "invoice-scans"))
    .WithContainer("avatars", "/mnt/shared/avatars"));

builder.AddOutbox<InvoiceDbContext>(
    outboxOptions => outboxOptions.Retention = TimeSpan.FromDays(7),
    config => config
        .WithMessage<InvoiceDraftedMessage, InvoiceDraftedMessageHandler>()
        .WithBlobs());   // required - every removal of content rides on the outbox

builder.AddBlobSweepRecurringTask(taskOptions =>
{
    taskOptions.InitialDelay = TimeSpan.FromMinutes(1);
    taskOptions.Period = TimeSpan.FromHours(1);
});
```

**Containers are separate, isolated entities.** Each one is registered on its own with `WithContainer`, with its own `BlobContainerOptions` (`MaxSize`, `AllowedContentTypes`, `AllowedExtensions`, `ComputeChecksum`, `OrphanGracePeriod`) and its own folder.

Storing a file is a step in a transaction, not a transaction of its own:

```csharp
var result = await blobStorage.AddOnSaveAsync(
    new BlobUploadRequest
    {
        Container = "invoice-scans",
        Content = file.OpenReadStream(),
        OriginalFileName = file.FileName,
        ContentType = file.ContentType,
    },
    cancellationToken);

invoice.AttachScan(result.Value.Id);
await context.SaveChangesAsync(cancellationToken); // the row and the aggregate, both or neither
```

`AddOnSaveAsync()` only tracks the row. **It never saves.** If the save rolls back, the file is later cleaned up (after the grace period ends). Keep the grace period comfortably longer than the slowest transaction that could be holding a blob.

Deleting works the same way:

```csharp
context.Invoices.Remove(invoice);
await blobStorage.DeleteOnSaveAsync(scan, cancellationToken);
await context.SaveChangesAsync(cancellationToken); // both, or neither
```

The files are stored in the container folder, named after their `BlobId`:

```
{containerFolder}/{blobId}

blobs/invoice-scans/0198b7c4-…
```

#### Immutability

A blob cannot be rewritten. There is no `LastModifiedAt` to keep honest and its checksum is a strong HTTP `ETag` for as long as it exists:

```csharp
Response.Headers.ETag = scan.Blob.Checksum?.ToETag();
return File(scan.Content, scan.Blob.ContentType.Value, scan.Blob.OriginalFileName);
```

Replacing a file means storing a new blob and enqueueing the deletion of the old one.

`Metadata` is the one mutable part - a small JSON column for facts the library cannot know, such as `width`/`height`. Anything you query on deserves a real column on an entity of your own.

#### The sweep

`IBlobSweeper` removes blobs whose `ExpiresAt` has passed, across every container.

Skip `AddBlobSweepRecurringTask` to drive `IBlobSweeper` from Quartz.NET or anything else. Skip it entirely and expired blobs simply stay.

## Demo App

Check out the [demo project](../MartinDrozdik.DDD.Demo) for examples.

It's structured for demo purposes, I would recommend structuring with vertical slices in a real app, but it shows all the features in one place.

It's a simple ASP.NET Core app with a few endpoints, using the mediator for commands and queries, and demonstrating the error handling and telemetry in action. Check it out for examples of how to use the library in a real app...
