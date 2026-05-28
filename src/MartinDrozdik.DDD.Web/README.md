# DDD for ASP.NET Core - Web Plumbing That Doesn't Suck

[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD.Web?style=flat-square&logo=nuget&label=MartinDrozdik.DDD.Web)](https://www.nuget.org/packages/MartinDrozdik.DDD.Web)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD.Web?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/MartinDrozdik.DDD.Web)
[![Build & Test](https://img.shields.io/github/actions/workflow/status/drozdik-m/domain-driven-design/main.yml?branch=main&style=flat-square&logo=github&label=actions)](https://github.com/drozdik-m/domain-driven-design/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/drozdik-m/domain-driven-design?style=flat-square)](LICENSE)

Opinionated web infrastructure for .NET based on [MartinDrozdik.DDD](../MartinDrozdik.DDD). Includes error handling, logging, telemetry, health checks, and other setup you'll need anyway. Check the [demo](../MartinDrozdik.DDD.Demo).

## Installation

```bash
dotnet add package MartinDrozdik.DDD.Web
```

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

Leverages `IOptions<T>` with FluentValidation for configuration validation and automatic binding.

Configured to **fail fast** if your configuration is invalid. No more *"wOrKs On My MaChInE"*.

```csharp
public class InvoiceOptions : IValidatedAppOptions<InvoiceOptions>
{
    public static string Section => "App:Invoice"; // Simple binding from config (appsettings etc.)

    // FluentValidation validator, because attributes are for suckers
    public static AbstractValidator<InvoiceOptions> Validator { get; } = new OptionsValidator();

    // Your actual options properties:
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

// Register it:
builder.Services.AddValidatedAppOptions<InvoiceOptions>();
```

For simpler options without validation:

```csharp
public class SimpleOptions : IAppOptions
{
    public static string Section => "App:Simple";
    public required string Value { get; init; }
}

builder.Services.AddAppOptions<SimpleOptions>();
```

### EEE (Ezy Error Ehndling)

Automatic conversion of DDD exceptions to HTTP responses:

- `BusinessRuleValidationException` → 400 Bad Request
- `ValidationException` (FluentValidation) → 400 Bad Request
- `BusinessNotFoundException` → 404 Not Found
- `BusinessRuleException` → 500 Internal Server Error (with business details)
- Anything else → 500 Internal Server Error

In **development, you get detailed info**. In **production, you get clean and safe** error messages. The middleware handles it:

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
await app.MigrateDatabaseAsync<YourDbContext>();
```

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

- **OnAggregatesSave**
- **OnDomainEntitiesSave**

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

## Demo App

Check out the [demo project](../MartinDrozdik.DDD.Demo) for examples.

It's structured for demo purposes, I would recommend structuring with vertical slices in a real app, but it shows all the features in one place.

It's a simple ASP.NET Core app with a few endpoints, using the mediator for commands and queries, and demonstrating the error handling and telemetry in action. Check it out for examples of how to use the library in a real app...
