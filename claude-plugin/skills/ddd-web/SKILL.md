---
description: Use when setting up or configuring MartinDrozdik.DDD.Web — AddAppServices, UseAppMiddlewares, validated options, EF Core setup with AddAppDbContext, health checks, OpenTelemetry, error handling middleware, DddDbContext, reverse proxy, or HTTP client resilience.
---

You are an expert in the **MartinDrozdik.DDD.Web** library. Help the user configure ASP.NET Core infrastructure using this specific library.

## Library philosophy

Everything is **optional and composable**. Use what helps, ignore the rest. The one-liner setup gives a production-ready baseline; individual extension methods let you pick exactly what you need.

Install: `dotnet add package MartinDrozdik.DDD.Web`

## User request

$ARGUMENTS

---

## Quick start — bare minimum

Two extension methods wire up logging, error handling, OpenAPI, health checks, OpenTelemetry, and HTTP resilience:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices();

var app = builder.Build();
app.UseAppMiddlewares();

await app.RunAsync();
```

Turn off individual modules via options:

```csharp
var options = MartinDrozdik.DDD.Web.WebApplicationOptions.Default with
{
    UseStaticFilePathProvider = false,
};
builder.AddAppServices(options);
```

## Modules

### Validated Options

Fail fast on bad config. Implement `IValidatedAppOptions<T>` with a FluentValidation validator:

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

// Register:
builder.Services.AddValidatedAppOptions<InvoiceOptions>();
```

For options that don't need validation, implement `IAppOptions`:

```csharp
public class SimpleOptions : IAppOptions
{
    public static string Section => "App:Simple";
    public required string Value { get; init; }
}

builder.Services.AddAppOptions<SimpleOptions>();
```

### Error Handling

Converts DDD exceptions to RFC 7807 HTTP responses automatically:

| Exception | HTTP status |
|---|---|
| `BusinessRuleValidationException` | 400 Bad Request |
| `ValidationException` (FluentValidation) | 400 Bad Request |
| `BusinessNotFoundException` | **404 Not Found** |
| `BusinessRuleException` | 500 Internal Server Error |
| Anything else | 500 Internal Server Error |

Development gets detailed error info; production gets clean, safe messages.

```csharp
builder.Services.AddAppErrorHandling();
// ...
app.UseExceptionHandler();
```

Your domain layer throws; the middleware translates. No HTTP plumbing in business logic.

### EF Core / Database

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
// Bind connection string from config automatically:
builder.AddAppDbContext<YourDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

// Or configure manually:
builder.AddAppDbContext<YourDbContext>(dbBuilder =>
{
    dbBuilder.UseSqlServer(connectionString);
});
```

Dev environment gets sensitive data logging and detailed errors; production does not.

Ensure the database exists on startup:

```csharp
await app.EnsureCreatedDatabaseAsync<YourDbContext>();
// or run pending migrations:
await app.MigrateDatabaseAsync<YourDbContext>();
```

### DddDbContext

Extended `DbContext` for DDD apps. Hooks fire around EF Core `SaveChanges`:

```csharp
public class InvoiceDbContext(DbContextOptions options, TimeProvider timeProvider)
    : DddDbContext(options)
{
    private const string CreatedAtPropertyName = "CreatedAt";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        // Add audit shadow properties to all aggregate roots
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
        {
            entry.Property(CreatedAtPropertyName).CurrentValue = now;
        }
    }
}
```

Available hooks: `OnAggregatesSave`, `OnDomainEntitiesSave`.

### Health Checks

```csharp
builder.AddAppHealthChecks();

// With custom checks:
builder.AddAppHealthChecks(checks =>
{
    checks.AddDbContextCheck<YourDbContext>();
});

app.MapAppHealthChecks(); // registers /health/live and /health/ready
```

### OpenTelemetry

```csharp
builder.AddAppOpenTelemetry();
```

Configure export via environment variables (compatible with Aspire out of the box):

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
OTEL_SERVICE_NAME=your-service
OTEL_SERVICE_VERSION=1.0.0
```

Health check requests are filtered from traces automatically.

### Reverse Proxy

When running behind nginx, YARP, or similar:

```csharp
app.IsBehindProxy(); // handles X-Forwarded-* headers
```

### HTTP Client Resilience

```csharp
builder.Services.AddHttpClientResilience();
```

Applies default retry and timeout policies to all HTTP clients.

### Static File Path Provider

Cache-busting query string versioning injected via `IStaticFilePathProvider`:

```csharp
IStaticFilePathProvider provider; // inject
provider.PathTo("app.js");
// Dev:  "app.js?version=<unix-timestamp>"  (busts every request)
// Prod: "app.js?version=1.2.3"             (busts on deploy)
```

```json
// appsettings.json
{
  "App": {
    "StaticFileVersioning": { "Version": "1.2.3" }
  }
}
```

## Reference

See the [demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) — Program.cs for full wiring, Context/ for DddDbContext examples.
