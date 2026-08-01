---
description: Use when setting up or configuring MartinDrozdik.DDD.Web — AddAppServices, UseAppMiddlewares, validated options, EF Core setup with AddAppDbContext, health checks, OpenTelemetry, error handling middleware, DddDbContext, recurring background tasks with AddRecurringTask, reverse proxy, or HTTP client resilience.
---

You are an expert in the **MartinDrozdik.DDD.Web** library. Generate correct ASP.NET Core infrastructure setup using its specific APIs.

## Request

$ARGUMENTS

---

**Every module is optional.** `AddAppServices()` / `UseAppMiddlewares()` register all of them as a bundle; call individual extension methods to include only what you need.

Install: `dotnet add package MartinDrozdik.DDD.Web`

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

Use `IValidatedAppOptions<T>` when options must be validated on startup (fail-fast). Use `IAppOptions` for options with no validation requirement.

```csharp
public class InvoiceOptions : IValidatedAppOptions<InvoiceOptions>
{
    public static string Section => "App:Invoice";
    public static AbstractValidator<InvoiceOptions> Validator { get; } = new OptionsValidator();

    public required int StartingId { get; init; }
    public required string DefaultName { get; init; }

    private class OptionsValidator : AbstractValidator<InvoiceOptions>
    {
        public OptionsValidator()
        {
            RuleFor(e => e.StartingId).GreaterThanOrEqualTo(0);
            RuleFor(e => e.DefaultName).NotNull().NotEmpty();
        }
    }
}

builder.Services.AddValidatedAppOptions<InvoiceOptions>();
```

```csharp
public class SimpleOptions : IAppOptions
{
    public static string Section => "App:Simple";
    public required string Value { get; init; }
}

builder.Services.AddAppOptions<SimpleOptions>();
```

Both bind strictly (`ErrorOnUnknownConfiguration = true`) and validate on start, so a typo in a config key is a
startup failure rather than a silent default.

To read options *during* startup, before the container exists, go through the configuration manager:

```csharp
var db = builder.Configuration.GetRequiredValidatedOptions<DatabaseOptions>();
// also: GetOptions<T>(), GetRequiredOptions<T>(), GetValidatedOptions<T>()
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
| `Timeout` | `null` | Cancels the iteration's token when it elapses; the loop moves on. |

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
