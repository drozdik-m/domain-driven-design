# Set of Domain-Driven Design (DDD) libraries for .NET

This repository contains a set of libraries that provide NuGet packages for Domain-Driven Design (DDD) in .NET. Because apparently we all need more packages in our lives.

*Setup your maintainable .NET website project today!.*

## MartinDrozdik.DDD

**A pragmatic .NET library for Domain-Driven Design that doesn't force you into abstract nonsense (that much).**

Contains basic interfaces and building blocks for DDD and quality code, such as:

- **ValueObject, Entity, AggregateRoot**, ... (the usual suspects)
- **Object-based enumerations**
- **Validation** and **error handling** (based on `FluentValidation`, not your tears)
- **Type-safe ID** patterns (no more Guid soup)
- **Mediator** for commands and queries (with handlers) – integrated via DI
  - And **pipelines**!
- Other goodies that **make DDD easier without forcing** you into a specific architecture or framework

Check out [very nice README.md](./src/MartinDrozdik.DDD/README.md) for this library and possibly the [demo](./src/MartinDrozdik.DDD.Demo) for examples.

[![DDD Logo](./src/MartinDrozdik.DDD/ddd-icon.png)](./src/MartinDrozdik.DDD/README.md)

## MartinDrozdik.DDD.Web

**Opinionated web infrastructure for ASP.NET Core that doesn't make you want to flip tables.**

Built on top of `MartinDrozdik.DDD`, this package provides all the web plumbing you'll need anyway:

- **One-liner setup** – `AddAppServices()` and `UseAppMiddlewares()` because life's too short for ceremony
- **Error handling**
- **Configuration validation** – with `FluentValidation`
- **Database helpers**
- **OpenTelemetry** – *Hello Aspire* (and any other OTLP-consumers)
- Basic **Health checks**
- **HTTP resilience**
- **OpenAPI**

*Everything is optional and composable.* Use what helps, ignore the rest. I won't tell.

Check out [very nice README.md](./src/MartinDrozdik.DDD.Web/README.md) for full details and examples that actually compile.

[![DDD Web Logo](./src/MartinDrozdik.DDD.Web/ddd-web-icon.png)](./src/MartinDrozdik.DDD.Web)

## MartinDrozdik.DDD.Demo

A demo application that shows recommended patterns for using the `MartinDrozdik.DDD` and `MartinDrozdik.DDD.Web` library. It's not gospel, but it works (which is more than you can say for most blog post code).

Check out:

- [Program.cs](./src/MartinDrozdik.DDD.Demo/Program.cs) – the entry point with minimal setup using the libraries
- [Models/](./src/MartinDrozdik.DDD.Demo/Models) – Aggregates, Entities, Value Objects, Enumerations
- [Requests/](./src/MartinDrozdik.DDD.Demo/Requests) – Commands and Queries with handlers
- [Context/](./src/MartinDrozdik.DDD.Demo/Context) – EF Core configuration with identity converters

The demo is structured for demonstration purposes. For real apps, consider vertical slice architecture. Or don't. You do you.

Check out [very nice README.md](./src/MartinDrozdik.DDD.Demo/README.md) for more details about the demo.

[![DDD Demo Logo](./src/MartinDrozdik.DDD.Demo/ddd-demo-icon.png)](./src/MartinDrozdik.DDD.Demo)
