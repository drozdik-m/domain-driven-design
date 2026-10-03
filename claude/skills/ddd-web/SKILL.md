---
description: Use when setting up or configuring MartinDrozdik.DDD.Web — AddAppServices, UseAppMiddlewares, EF Core setup with AddAppDbContext, health checks, OpenTelemetry, error handling middleware, DddDbContext, recurring background tasks with AddRecurringTask, the transactional outbox with AddOutbox/IOutbox<TContext>/IOutboxMessage, reverse proxy, or HTTP client resilience.
---

You are an expert in the **MartinDrozdik.DDD.Web** library. Generate correct ASP.NET Core infrastructure setup using its specific APIs.

## Request

$ARGUMENTS

---

**Every module is optional.** `AddAppServices()` / `UseAppMiddlewares()` register all of them as a bundle; call individual extension methods to include only what you need.

Install: `dotnet add package MartinDrozdik.DDD.Web`

---

## Endpoint results: `TypedResults`, never `Results`

Always write `TypedResults.Ok(...)`, `TypedResults.NotFound()`, `TypedResults.Problem(...)`,
`TypedResults.ValidationProblem(...)` — in minimal API endpoints, in `IExceptionHandler`
implementations, and anywhere else an `IResult` is produced.

**Never use the static `Results` class.** It clashes with the `MartinDrozdik.DDD.Results` namespace.
`TypedResults` is the right call regardless: it returns concrete types.

```csharp
// Do
return TypedResults.Ok(invoice);
return TypedResults.NotFound();
return TypedResults.Problem(
    detail: message,
    statusCode: StatusCodes.Status500InternalServerError);

// Don't — does not compile under a MartinDrozdik.DDD.* namespace
return Results.Ok(invoice);
```

MVC controllers are unaffected: `ControllerBase.Ok()`, `NotFound()`, `Problem()` and `ActionResult<T>` are
instance members, not the static `Results` class, so they keep working as written.

---

## Quick start

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices(); // logging, error handling, OpenAPI, health checks, OTEL, HTTP resilience

var app = builder.Build();
app.UseAppMiddlewares();

await app.RunAsync();
```

Opt out of individual modules:

```csharp
builder.AddAppServices(WebApplicationOptions.Default with
{
    UseStaticFilePathProvider = false,
});
```

---

## Modules

### Validated Options

At `MartinDrozdik.DDD.Options` — use the `ddd-options` skill. This package adds only:

```csharp
builder.WebHost.SetOption<InvoiceOptions>(e => e.DefaultName, "Invoice"); // strongly-typed UseSetting
```

### Error Handling

Converts DDD exceptions to RFC 7807 HTTP responses. Domain layer throws; middleware translates. No HTTP concerns in business logic.

| Exception | HTTP status | Handler |
|---|---|---|
| `BusinessRuleValidationException` | 400 Bad Request | `BusinessRuleValidationExceptionHandler` |
| `BusinessNotFoundException` | **404 Not Found** | `BusinessNotFoundExceptionHandler` |
| `ValidationException` (FluentValidation) | 400 Bad Request | `ValidationExceptionHandler` |
| `BusinessRuleException` | 500 Internal Server Error | `GlobalExceptionHandler` (no dedicated handler) |
| Anything else | 500 Internal Server Error | `GlobalExceptionHandler` |

Handlers are tried in that order, so the two `BusinessRuleException` subclasses must be registered ahead of
the base type — `AddAppErrorHandling()` already does this.

Development responses add stack traces and exception details. Note that `GlobalExceptionHandler` currently
puts the raw `exception.Message` in the response **in every environment**, so do not put anything sensitive in
the message of an exception you expect to escape a handler.

```csharp
builder.Services.AddAppErrorHandling();
// ...
app.UseExceptionHandler();
```

### EF Core / Database

```json
{
  "App": {
    "Database": {
      "ConnectionString": "Data Source=app.db"
    }
  }
}
```

```csharp
// Auto-binds connection string from App:Database:ConnectionString
builder.AddAppDbContext<YourDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

// Manual configuration (no DatabaseOptions binding)
builder.AddAppDbContext<YourDbContext>(dbBuilder =>
{
    dbBuilder.UseSqlServer(connectionString);
});

// With DatabaseOptions *and* the scope's IServiceProvider — for interceptors resolved from DI
builder.AddAppDbContext<YourDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor>());
});
```

Dev: sensitive data logging + detailed errors. Production: neither.

```csharp
await app.EnsureCreatedDatabaseAsync<YourDbContext>();  // ensure DB exists
await app.EnsureMigratedDatabaseAsync<YourDbContext>(); // run pending migrations
await app.EnsureDeletedDatabaseAsync<YourDbContext>();  // drop it (tests / local reset)
```

### DddDbContext

Extend `DddDbContext` when you need lifecycle hooks on EF Core `SaveChanges`. It wires **three** overridable
hooks through every `SaveChanges`/`SaveChangesAsync` overload, each receiving the matching changed entries:

| Hook | Receives |
|---|---|
| `OnAggregatesSave` | entries whose type is an `IAggregateRoot<T>` |
| `OnDomainEntitiesSave` | entries whose type is an `IDomainEntity<T>` |
| `OnObjectsSave` | every changed entry, whatever it is |

A hook is skipped entirely when its set is empty. Entries come from `ChangeTracker.Entries()`, so they include
`Unchanged` ones — always filter on `EntityState` inside the hook, as below.

```csharp
public class InvoiceDbContext(DbContextOptions options, TimeProvider timeProvider)
    : DddDbContext(options)
{
    private const string CreatedAtPropertyName = "CreatedAt";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetAggregateRoots())
        {
            modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTimeOffset>(CreatedAtPropertyName);
        }
    }

    protected override void OnAggregatesSave(IEnumerable<EntityEntry> entityEntries)
    {
        base.OnAggregatesSave(entityEntries);
        var now = timeProvider.GetUtcNow();
        foreach (var entry in entityEntries.Where(e => e.State == EntityState.Added))
            entry.Property(CreatedAtPropertyName).CurrentValue = now;
    }
}
```

### Health Checks

`MapAppHealthChecks()` maps three endpoints under the `/health` prefix (`WebApplicationExtensions.HealthPathPrefix`):

| Endpoint | Runs |
|---|---|
| `/health/live` | checks tagged `live` — includes the built-in `"self"` check |
| `/health/ready` | checks tagged `ready` |
| `/health` | every registered check |

```csharp
builder.AddAppHealthChecks();

builder.AddAppHealthChecks(checks =>
{
    checks.AddDbContextCheck<YourDbContext>();
});

app.MapAppHealthChecks();
```

### Recurring Tasks

Background work on a schedule, without writing a `BackgroundService`. Implement `IRecurringTask` and register it:

```csharp
public class CleanupTask(InvoiceDbContext context, ILogger<CleanupTask> logger) : IRecurringTask
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // one iteration; respect the token and return promptly when it is cancelled
    }
}

builder.AddRecurringTask<CleanupTask>(options =>
{
    options.InitialDelay = TimeSpan.FromSeconds(30);
    options.Period = TimeSpan.FromMinutes(10);
    options.Timeout = TimeSpan.FromMinutes(2);
});
```

The task is registered **scoped** and resolved from a fresh DI scope every iteration, so a `DbContext` can be
constructor-injected as usual. A failing iteration is logged and the loop carries on.

`RecurringTaskOptions<TTask>` — configured in code only, there is **no configuration-binding overload**:

| Property | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Read once at startup. A disabled task never starts and cannot be triggered. |
| `InitialDelay` | `TimeSpan.Zero` | Wait after startup before the first iteration. |
| `Period` | `5 min` | Gap measured from when the **previous iteration finished**, so iterations never overlap and cannot back up. |
| `Timeout` | `null` | Cancels the iteration's token and logs a warning when it elapses; the loop continues once the iteration returns (no overlap, so a task ignoring its token holds up the loop). |

To run a task off-schedule, inject `IRecurringTaskTrigger<TTask>` anywhere — a controller, a handler, another task:

```csharp
public class InvoicesController(IRecurringTaskTrigger<CleanupTask> trigger) : ControllerBase
{
    [HttpPost("cleanup")]
    public IActionResult Cleanup()
    {
        trigger.Trigger();  // returns immediately
        return Accepted();
    }
}
```

`Trigger()` is non-blocking and **coalescing**: while an iteration is pending or running, further calls collapse
into the one pending request, and a request raised mid-iteration is honoured after it finishes.

`services.RemoveRecurringTasks()` strips every recurring-task loop while leaving other hosted services and the
tasks themselves registered. Integration tests get this for free — see the `ddd-testing` skill.

### Outbox

A transactional outbox over the same `DbContext` as your aggregates: the message row and the aggregate change
are written by **one** `SaveChangesAsync`, so a side effect can never be half-done. A background loop delivers
afterwards, at least once.

Use it whenever a handler both changes state and causes something outside the database (email, webhook, another
service). Not for work that is purely inside the same transaction.

**Contracts live in the core package** (`MartinDrozdik.DDD.Outbox`): `IOutbox<TContext>`, `IOutboxMessage`,
`IOutboxMessageHandler<T>`, `OutboxMessageType`, `OutboxException`. Everything else — storage, engine, options,
health check — is `MartinDrozdik.DDD.Web.Outbox`. A business-layer handler therefore only needs `MartinDrozdik.DDD`.

```csharp
// The message: a plain serializable record. Everything must round-trip through System.Text.Json.
public sealed record InvoiceDraftedMessage(Guid InvoiceId, string InvoiceNumber, string Recipient) : IOutboxMessage
{
    public static OutboxMessageType MessageType => "invoice.drafted.v1";
}

// The handler: resolved from a fresh DI scope per message. No try/catch, no bookkeeping — throwing means "retry".
public class InvoiceDraftedMessageHandler(IEmailSender sender) : IOutboxMessageHandler<InvoiceDraftedMessage>
{
    public Task HandleAsync(InvoiceDraftedMessage message, CancellationToken cancellationToken)
        => sender.SendAsync(message.Recipient, $"Invoice {message.InvoiceNumber} is ready.", cancellationToken);
}
```

Four pieces of wiring, all required except where noted:

```csharp
// 1. Map the table into the context that owns the aggregates — same DB, same transaction
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    modelBuilder.AddOutbox();                       // table "OutboxMessage"; AddOutbox("Messages", "outbox") to move it
}

// 2. The engine + every message type. configureOptions is positional and NOT optional — pass null for defaults.
builder.AddOutbox<InvoiceDbContext>(
    outboxOptions => outboxOptions.Retention = TimeSpan.FromDays(7),
    config => config
        .WithMessage<InvoiceDraftedMessage, InvoiceDraftedMessageHandler>()
        .WithMessage<InvoicePaidMessage, InvoicePaidMessageHandler>());

// 3. The schedule. Skip it only when driving IOutboxProcessor from Quartz.NET/Hangfire/cron yourself.
builder.AddOutboxDispatchRecurringTask(schedule =>
{
    schedule.InitialDelay = TimeSpan.FromSeconds(10);
    schedule.Period = TimeSpan.FromSeconds(30);
});

// 4. Optional: dispatch right after the commit instead of at the next poll
builder.AddAppDbContext<InvoiceDbContext>((options, provider, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
    dbBuilder.AddInterceptors(provider.GetRequiredService<OutboxTaskTriggerInterceptor>());
});
```

Enqueue from the command handler (or anywhere else inside the transaction) and let the existing save commit it:

```csharp
public class CreateInvoiceDraftCommandHandler(InvoiceDbContext context, IOutbox<InvoiceDbContext> outbox)
    : ICommandHandler<CreateInvoiceDraftCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(CreateInvoiceDraftCommand command, CancellationToken cancellationToken)
    {
        var invoice = Invoice.CreateDraft(issuer, recipient, invoiceNumber);
        await context.Invoices.AddAsync(invoice, cancellationToken);

        outbox.AddOnSave(new InvoiceDraftedMessage(invoice.Id.Key, invoiceNumber.ToString(), recipient.FullName));

        await context.SaveChangesAsync(cancellationToken);   // aggregate + message, both or neither
        return invoice.Id;
    }
}
```

`AddOnSave()` **only tracks the row — it never saves**, and it throws `OutboxException` when the type has no registered
handler or the payload exceeds `MaxPayloadLength`. Never call `SaveChangesAsync` just to flush a message.

#### Same context as the aggregate — always check

The outbox only works if the message and the aggregate are saved by **one** `SaveChangesAsync` on **one** context
instance. Getting this wrong loses messages silently. Check every time you wire or use it:

- **Registration** — `AddOutbox<TDbContext>` names the context that owns the aggregates, and `modelBuilder.AddOutbox()`
  is called in the `OnModelCreating` of that same context. Anything else riding on the outbox is registered over
  it too: `AddBlobs<InvoiceDbContext>` pairs with `AddOutbox<InvoiceDbContext>` (a mismatch fails to resolve).
- **One outbox per application** — call `AddOutbox` once, registering every message type in that call. A second
  call throws, even over the same context.
- **Handlers** — inject `IOutbox<InvoiceDbContext>` next to `InvoiceDbContext` and save with that context. Never pair
  it with a context from `IDbContextFactory`, a hand-rented pool instance or `new` — same type, different instance,
  and its save never sees the tracked message. Types cannot catch that one.
- **Several contexts** — if the aggregate lives in a context without the outbox, the outbox cannot cover that change.
  Move the aggregate (or the outbox) so they share a context; never add a second save on another context
  "for the message".

#### Versioning the message type — always end the key with `.v1`

The key maps a stored row back to a CLR type, so **renaming it strands every existing row**. Never derive it from a
type name and never rename it; bump it instead.

```csharp
public static OutboxMessageType MessageType => "invoice.drafted.v1";   // shipped

// Breaking change (Recipient is now required) → new type, next key, both registered until the v1 rows drain
public sealed record InvoiceDraftedV2Message(Guid InvoiceId, string InvoiceNumber, string Recipient) : IOutboxMessage
{
    public static OutboxMessageType MessageType => "invoice.drafted.v2";
}

config.WithMessage<InvoiceDraftedMessage, InvoiceDraftedMessageHandler>()       // drains old rows, remove later
      .WithMessage<InvoiceDraftedV2Message, InvoiceDraftedV2MessageHandler>();
```

Adding an optional member keeps the key (an old payload still deserializes). Anything a stored v1 payload cannot
satisfy bumps it. **Write `.v1` on the very first version** — a key with no version segment has nowhere to go.

Keys are `[A-Za-z0-9._-]`, at most 100 characters, and compared case-insensitively; a duplicate key throws
`OutboxException` while the container is being built.

`OutboxOptions` — configured in code, validated on start:

| Property | Default | Meaning |
|---|---|---|
| `BatchSize` | `100` | Messages delivered per `ProcessPendingAsync` call. |
| `LeaseDuration` | `5 min` | How long a claim is honoured. Must exceed the slowest handler, or another processor takes the message mid-delivery. |
| `RetryDelays` | `10s, 1m, 5m, 30m` | Backoff schedule. **List length = number of retries**; empty dead-letters on the first failure. |
| `MaxPayloadLength` | `null` | Cap on serialized payload characters. `AddOnSave()` throws over it, before anything is saved. |
| `Retention` | `null` | How long *delivered* messages are kept. Dead-lettered ones are never deleted automatically. |
| `SerializerOptions` | `JsonSerializerOptions.Web` | Changing it after rows exist can make payloads unreadable. |

Delivery semantics, worth stating in code review:

- **At-least-once — handlers must be idempotent.** A crash after the handler ran but before the row was marked
  redelivers the message.
- Claims use a lease plus an optimistic concurrency token, so concurrent processors and multiple instances are safe.
- Handler throws → retried on the backoff; retries exhausted → dead-lettered (`FailedAt` set), kept for inspection,
  never delivered again.
- An unregistered message type is retried, not dead-lettered on sight (rolling deploys see each other's messages).
- `OperationCanceledException` on shutdown is not a failure: the lease simply expires.

Health check, reporting `pending` / `overdue` / `deadLettered`, unhealthy on any dead-letter, degraded above the
backlog threshold:

```csharp
builder.AddAppHealthChecks(checks => checks.AddOutboxHealthCheck<InvoiceDbContext>(degradedBacklogThreshold: 1000));
```

Testing an outbox is in the `ddd-testing` skill — every app with an outbox gets an `OutboxSmokeTests<Program, TDbContext>`.

### OpenTelemetry

```csharp
builder.AddAppOpenTelemetry();
```

Configure via environment variables (compatible with Aspire out of the box):

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
OTEL_SERVICE_NAME=your-service
OTEL_SERVICE_VERSION=1.0.0
```

Health check requests are excluded from traces automatically.

### Reverse Proxy

```csharp
app.IsBehindProxy(); // processes X-Forwarded-* headers (nginx, YARP, etc.)
```

### HTTP Client Resilience

```csharp
builder.Services.AddHttpClientResilience(); // default retry + timeout policies
```

### Static File Path Provider

Cache-busting via query string versioning. Inject `IStaticFilePathProvider`:

```csharp
provider.PathTo("app.js");
// Development: "app.js?version=<unix-timestamp>"  — busts on every request
// Production:  "app.js?version=1.2.3"             — busts on deploy
```

```json
{
  "App": {
    "StaticFileVersioning": { "Version": "1.2.3" }
  }
}
```

`Version` is required in **every** environment, including Development, even though the development provider
ignores it and stamps a timestamp instead. Use `AddIdentityStaticFilePathProvider()` for a no-op provider that
returns the path unchanged.

### Smaller pieces

```csharp
builder.AddAppLogging(LogLevel.Debug);   // defaults to Information
```

Adds console logging everywhere, plus debug output and full HTTP request logging in Development and Testing.

`AddAppServices()` calls this for you at the default level; call it directly only to change the level or when
composing modules individually.

The library adds a `"Testing"` environment alongside the built-in three, which is what the testing package runs
apps under:

```csharp
if (app.Environment.IsTesting()) { ... }   // MartinDrozdik.DDD.Web.Environments
AppEnvironments.Testing                     // the "Testing" string constant
```

OpenAPI schema naming, for when generic or nested types produce collisions:

```csharp
builder.Services.AddAppOpenApi(options => options.ParentDeclarationSchemaIds());
builder.Services.AddAppOpenApi(options => options.CustomSchemaIds(type => type.Name));
```

EF Core model introspection by DDD role, useful in `OnModelCreating` for role-wide conventions:

```csharp
modelBuilder.Model.GetAggregateRoots();   // IEnumerable<IMutableEntityType>
modelBuilder.Model.GetDomainEntities();
```

## Reference

[Demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) — Program.cs for full startup wiring, Context/ for DddDbContext examples.
