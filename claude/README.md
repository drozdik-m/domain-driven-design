# MartinDrozdik.DDD — Claude Code Plugin

Claude Code skills for the [MartinDrozdik.DDD](https://github.com/drozdik-m/domain-driven-design) library family. Gives Claude deep, accurate knowledge of the library's APIs and patterns so it can generate correct code without hallucinating method signatures or missing conventions.

## Skills

| Skill | Slash command | Covers |
| --- | --- | --- |
| **ddd** | `/martin-drozdik-ddd:ddd` | ValueObject, Entity, AggregateRoot, strongly-typed IDs, Enumerations, Specifications, error handling, CQRS Mediator |
| **ddd-web** | `/martin-drozdik-ddd:ddd-web` | `AddAppServices`, `UseAppMiddlewares`, validated options, EF Core setup, health checks, OpenTelemetry, `DddDbContext` |
| **ddd-testing** | `/martin-drozdik-ddd:ddd-testing` | `TestedApp`, `TestedAppBuilder`, smoke tests, EF Core integration tests, `EqualityAssert`, `ResultAssert` |

Skills also trigger automatically when Claude detects you are working with these libraries.

## Installation

### 1. Add this repository as a marketplace

``` bash
/plugin marketplace add drozdik-m/domain-driven-design/claude-plugin
```

### 2. Install the plugin

``` bash
/plugin install martin-drozdik-ddd@domain-driven-design
```

### 3. Verify

```bash
/martin-drozdik-ddd:ddd
/martin-drozdik-ddd:ddd-web
/martin-drozdik-ddd:ddd-testing
```

## Usage

Invoke a skill directly with an optional request:

```bash
/martin-drozdik-ddd:ddd create an Invoice aggregate with a strongly-typed ID and state enumeration
/martin-drozdik-ddd:ddd-web set up EF Core with DddDbContext and audit timestamps
/martin-drozdik-ddd:ddd-testing write smoke tests and an EF Core integration test for InvoiceDbContext
```

Or just ask Claude naturally — skills auto-trigger when you are working with these libraries.

## NuGet packages

```bash
dotnet add package MartinDrozdik.DDD
dotnet add package MartinDrozdik.DDD.Web
dotnet add package MartinDrozdik.DDD.Testing
```

## License

MIT
