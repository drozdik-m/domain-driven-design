---
description: Use when setting up or configuring MartinDrozdik.DDD.Web — AddAppServices, UseAppMiddlewares, validated options, EF Core setup with AddAppDbContext, health checks, OpenTelemetry, error handling middleware, DddDbContext, reverse proxy, or HTTP client resilience.
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

### Error Handling

Converts DDD exceptions to RFC 7807 HTTP responses. Domain layer throws; middleware translates. No HTTP concerns in business logic.

| Exception | HTTP status |
|---|---|
| `BusinessRuleValidationException` | 400 Bad Request |
| `ValidationException` (FluentValidation) | 400 Bad Request |
| `BusinessNotFoundException` | **404 Not Found** |
| `BusinessRuleException` | 500 Internal Server Error |
| Anything else | 500 Internal Server Error |

Development responses include stack traces and details; production responses are clean and safe.

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
await app.EnsureCreatedDatabaseAsync<YourDbContext>(); // ensure DB exists
await app.MigrateDatabaseAsync<YourDbContext>();        // run pending migrations
```

### DddDbContext

Extend `DddDbContext` when you need lifecycle hooks on EF Core `SaveChanges`. Two overridable hooks: `OnAggregatesSave`, `OnDomainEntitiesSave`.

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

Registers `/health/live` and `/health/ready` endpoints.

```csharp
builder.AddAppHealthChecks();

builder.AddAppHealthChecks(checks =>
{
    checks.AddDbContextCheck<YourDbContext>();
});

app.MapAppHealthChecks();
```

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

## Reference

[Demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) — Program.cs for full startup wiring, Context/ for DddDbContext examples.
