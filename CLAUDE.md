# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Planned improvements, known bugs, and open questions are tracked per package in [ROADMAP.md](./ROADMAP.md). Consult and update it when working on review findings.

## Build & Test

Solution file: `src/MartinDrozdik.DDD.slnx` (modern `.slnx` format, requires VS 2022+ or .NET 10 CLI).

```powershell
# Build
dotnet build src/MartinDrozdik.DDD.slnx

# Run all tests
dotnet test src/MartinDrozdik.DDD.slnx

# Run tests for a single project
dotnet test src/MartinDrozdik.DDD.Tests/MartinDrozdik.DDD.Tests.csproj
dotnet test src/MartinDrozdik.DDD.Web.Tests/MartinDrozdik.DDD.Web.Tests.csproj
dotnet test src/MartinDrozdik.DDD.Demo.Tests/MartinDrozdik.DDD.Demo.Tests.csproj

# Pack NuGet packages (auto-generated on build via GeneratePackageOnBuild)
dotnet pack src/MartinDrozdik.DDD/MartinDrozdik.DDD.csproj --configuration Release
```

## Architecture

This repo publishes three NuGet packages built on top of each other:

```
MartinDrozdik.DDD          ← core DDD primitives
MartinDrozdik.DDD.Web      ← ASP.NET Core infrastructure (depends on core)
MartinDrozdik.DDD.Testing  ← xUnit test helpers (depends on Web)
```

`MartinDrozdik.DDD.Demo` is a reference application; `MartinDrozdik.DDD.Demo.Tests` exercises it end-to-end.

### Core library (`MartinDrozdik.DDD`)

| Namespace area | What lives there |
|---|---|
| `Templates` | `ValueObject`, `IDomainEntity<TIdentity>`, `IAggregateRoot<TIdentity>` base classes/interfaces |
| `Identities` | Strongly-typed ID wrappers: `GuidIdentity<T>`, `IntIdentity<T>`, `StringIdentity<T>` + EF Core converters |
| `Specifications` | `ISpecification<TContext>` returning `SpecificationResult`; composable via `And`/`Or`/`Not`/`Tautology`/`Contradiction` |
| `Errors` | `Error`, `ErrorBuilder` (fluent), `Result<T>` |
| `Exceptions` | `BusinessRuleException`, `ValidationException` |
| `Enumerations` | `Enumeration` base class — object-oriented enums with behavior and properties |
| `Mediator` | `ICommand<TResponse>` + `ICommandHandler`, `IQuery<TResponse>` + `IQueryHandler`; pipeline integrators: `LoggingPipelineIntegrator`, `ValidationPipelineIntegrator` |

### Web library (`MartinDrozdik.DDD.Web`)

Entry point is two extension methods: `AddAppServices()` and `UseAppMiddlewares()`. Every module (OpenTelemetry, health checks, HTTP resilience, OpenAPI, RFC 7807 error formatting, FluentValidation config validation) is optional and composable.

### Testing library (`MartinDrozdik.DDD.Testing`)

- `TestedApp<TProgram>` / `TestedAppBuilder<TProgram>` — fluent wrapper around `WebApplicationFactory` for integration tests
- Free base classes for smoke tests (health check, OpenAPI, error handling)
- EF Core helpers: entity mapping, migration, connectivity, and model compilation assertions

## Coding conventions

- **File-scoped namespaces** everywhere.
- **XML doc comments** must use `<see cref="X"/>` for any referenced types/members.
- Use `nameof()` for any string that refers to a method, property, or parameter name.
- **Test method naming**: sentence description in `snake_case`, e.g. `Equals_successfully_returns_true_for_equal_parameters`.
- **Test structure**: three commented sections — `// Arrange`, `// Act`, `// Assert`.
- Test classes end with the `Tests` suffix.
- Test framework: **xUnit v3** (newest version).

## Demo as reference

When looking for usage examples, check `src/MartinDrozdik.DDD.Demo/`:

- `Models/` — Aggregate, Entity, ValueObject, Enumeration examples
- `Requests/` — Command/Query handlers
- `Context/` — EF Core setup with strongly-typed ID converters
- `Program.cs` — minimal startup using `AddAppServices()` / `UseAppMiddlewares()`
