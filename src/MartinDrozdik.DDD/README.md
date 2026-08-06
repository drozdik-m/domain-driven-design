# Flexible Domain-Driven Design (DDD) library for .NET

[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD?style=flat-square&logo=nuget&label=MartinDrozdik.DDD)](https://www.nuget.org/packages/MartinDrozdik.DDD)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/MartinDrozdik.DDD)
[![Build & Test](https://img.shields.io/github/actions/workflow/status/drozdik-m/domain-driven-design/main.yml?branch=main&style=flat-square&logo=github&label=actions)](https://github.com/drozdik-m/domain-driven-design/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/drozdik-m/domain-driven-design?style=flat-square)](https://github.com/drozdik-m/domain-driven-design/blob/main/LICENSE)

A pragmatic .NET library for Domain-Driven Design that doesn't force you into abstract nonsense (that much). Check the [demo](../MartinDrozdik.DDD.Demo).

## Installation

```bash
dotnet add package MartinDrozdik.DDD
```

*duh...*

Also check this repos' [DDD Claude Code plugin](../../claude/README.md) for better AI code generation.

## Philosophy

- **Pragmatic over purist** - Use what makes sense, ignore what doesn't
- **Composition over inheritance** - Depth is the enemy of maintainability
- **Explicit over implicit** - Code should be obvious, not something you need to explain in a meeting
- **Fewer layers, sharper boundaries** – Layers exist to solve problems, not to satisfy diagrams or blog posts
- **Type safety** - Compiler errors > Runtime errors (reflection is for edge cases, not architecture)
- **Fail fast** - Built with validation and error handling in mind

*Use the parts that help. Ignore the rest. Good luck.*

## Demo Project

The demo project shows recommended patterns. It's not gospel, but it works. Check out:

- [Models/](../MartinDrozdik.DDD.Demo/Models) - Aggregates, Entities, Value Objects, Enumerations
- [Requests/](../MartinDrozdik.DDD.Demo/Requests) - Commands and Queries with handlers
- [Context/](../MartinDrozdik.DDD.Demo/Context) - EF Core configuration with identity converters

## Templates (keeping it simple, stupid)

**Basic interfaces that define DDD building blocks.** These are interfaces, not abstract classes, because your ORM probably needs that flexibility anyway. Also, no base classes with 100 methods you don't need. Just the essentials.

Check out the [demo](../MartinDrozdik.DDD.Demo) for more examples with validation and other goodies:

- [Person entity with validation and strongly typed ID](../MartinDrozdik.DDD.Demo/Models/Entities/Person.cs)
- [Invoice aggregate with validation and strongly typed ID](../MartinDrozdik.DDD.Demo/Models/Aggregates/Invoice.cs)
- [Value Object with validation for an Invoice Number](../MartinDrozdik.DDD.Demo/Models/ValueObjects/InvoiceNumber.cs)
- [In-Memory state enumeration](../MartinDrozdik.DDD.Demo/Models/Enumerations/InvoiceState.cs)

### Value Objects

Compared by value, not identity:

```csharp
public class InvoiceNumber : ValueObject
{
    private InvoiceNumber(int year, int order)
    {
        Year = year;
        Order = order;
    }

    public int Year { get; }

    public int Order { get; }

    public static InvoiceNumber Create(int year, int order)
    {
        var result = new InvoiceNumber(year, order);
        return result;
    }

    // Takes care of equality and hashing based on the properties you yield here:
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Order;
    }
}
```

### Entities

Compared by identity, not value. Just implement the marker interface and you're good to go:

```csharp
public class Person : IDomainEntity<PersonId> // or IAggregateRoot<PersonId>
{
    private Person(PersonId id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public PersonId Id { get; }
    public string FullName { get; }

    public static Person Create(string fullName)
    {
        // Validate...
        return new Person(new PersonId(Guid.CreateVersion7()), fullName);
    }
}
```

### Aggregates

Your consistency boundaries (aka "units of work that make sense").

*Same implementation as Entity, just a different marker interface `IAggregateRoot<Guid>`.*

### Identities

Strongly-Typed IDs (no more `Guid` soup).

Stop passing around naked `Guid`s and `int`s like it's 2010. Type-safe identities with conversion support:

```csharp
public class PersonId(Guid key) : GuidIdentity<PersonId>(key);

// Usage:
interface ISomeService
{
    Person Get(PersonId id);
}
var invoice = someService.Get(id); // Type-safe!
```

Built-in primitives:

- `GuidIdentity<T>` - For when you want GUIDs (use `Guid.CreateVersion7()` like a civilized person)
- `IntIdentity<T>` - For when you're stuck with legacy databases
- `StringIdentity<T>` - For when external systems hate you

EF Core support:

```csharp
builder.Property(e => e.Id)
    .HasIdentityConvertor(new IdentityConverter<InvoiceId, Guid>());
```

### Enumerations

Because `enum` is fine until you need behavior and more properties, then you're screwed.

```csharp
public class InvoiceState(EnumerationName name /*and more properties as you like*/)
    : Enumeration(name)
{
    public static InvoiceState Draft => new(new EnumerationName("Draft"));
    public static InvoiceState Issued => new(new EnumerationName("Issued"));
    public static InvoiceState Paid => new(new EnumerationName("Paid"));
    
    // Add methods, validations, whatever you need
    public bool CanBeModified() => this == Draft;
}

// Usage
var state = InvoiceState.Draft;
if (state.CanBeModified()) { /* ... */ }
```

Serializes to strings/properties in your DB/JSON. Extends like a class. It's beautiful 😍.

**`Enumeration` is a `ValueObject`**, so equality comes for free – only the name is compared **case-sensitively**.

#### Exposing enumerations through an API

Your enumeration is a domain type. You don't want it on the public API surface, so you declare a plain `enum` for the contract – and then you're stuck writing a mapping method and keeping it in sync by hand. *Don't.*

```csharp
public enum InvoiceStateDto
{
    Draft,
    Issued,
    Paid,
}

// domain -> API
State = invoice.State.ToStructEnum<InvoiceStateDto>(),

// API -> domain
var state = InvoiceState.FromStructEnum(dto.State);
```

Members are matched **by name, case-sensitively**. Both directions have `...Optional` sibling that maps `null` to `null`: `ToStructEnumOptional` and `FromStructEnumOptional`.

When the names must differ, tell it directly:

```csharp
public enum InvoiceStateDto
{
    Draft,
    Issued,

    [EnumerationName("Paid")]
    Settled,
}
```

Check the whole mapping up front, at startup or from a test:

```csharp
EnumerationStructMapping.ThrowIfIncomplete<InvoiceState, InvoiceStateDto>();
```

Also works with `FluentValidation`:

```csharp
RuleFor(x => x.State).MustMapToEnumeration(EnumerationMap.To<InvoiceState>());
```

### Specifications

Because `bool` is fine until someone asks *"but why did it fail?"* Or even worse, your `if` condition is 10 lines of copy-pasta.

The **Specification Pattern** – a named DDD concept, not something I invented at 3am – lets you encapsulate business rules as composable, reusable objects that evaluate a context and tell you whether it passes, and *why* it doesn't.

Define a specification:

```csharp
private class IsDraftSpecification : ISpecification<Invoice>
{
    public SpecificationResult IsSatisfiedBy(Invoice invoice)
    {
        if (invoice.State != InvoiceState.Draft)
        {
            return new ErrorBuilder()
                .WithCode("InvoiceMustBeDraft")
                .WithMessage($"The invoice must be in the {InvoiceState.Draft} state.")
                .Build();
        }

        return SpecificationResult.Satisfied;
    }
}
```

`SpecificationResult` implicitly converts to `bool` and supports *boolean operations*, so simple checks stay simple:

```csharp
var spec = new OrderTotalGreaterThan(100);

// Simple boolean path - no ceremony
if (!spec.IsSatisfiedBy(context))
    return;

// Richer path
var result = spec.IsSatisfiedBy(context);
if (!result)
    return result.Errors; // IReadOnlyList<Error> explaining exactly what went wrong

// Ultimate megatron evolution path
if (!spec.TrySatisfyBy(this, out var specResult))
{
    throw new ErrorBuilder()
        .WithCode("CannotChangeIssuer")
        .WithMessage("The issuer of the invoice cannot be changed.")
        .WithSpecificationResult(specResult)
        .BuildValidationException();
}
```

**Composition** – one rule is never enough. Chain them fluently or use the classes directly:

```csharp
// Fluent — reads like a sentence, composes like Lego
var spec = new OrderTotalGreaterThan(100)
    .And(new CustomerIsVip())
    .Or(new CustomerActiveYears(5));

// Or use the classes directly if you hate fluent APIs (I will judge tho)
var spec = new AndSpecification<OrderContext>(
    new OrderTotalGreaterThan(100),
    new CustomerIsVip());
```

All composition operators are available. `&`/`|` aggregate errors from both sides (greedy). `&&`/`||` short-circuit when the outcome is deterministic (as it should be). Check out the docs at [Boolean logical operators - AND, OR, NOT, XOR](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators).

**Negation** — for when you want the opposite of a rule, with your own error message:

```csharp
var notVip = new CustomerIsVip().Not(new ErrorBuilder()
    .WithCode("CustomerIsVip")
    .WithMessage("VIP customers are not eligible for this offer.")
    .Build());
```

**Tautology & Contradiction** — for feature flags, defaults, and other situations where the rule is cosmically predetermined:

```csharp
// Always true
var permissive = TautologySpecification<OrderContext>.Instance;

// Always false, with a custom error
var disabled = new ContradictionSpecification<OrderContext>(someError);
```

## Exceptions and Errors

*Stuff happens. Sometimes it's your fault. Sometimes it's the network. Sometimes it's both.*

This library supports both **`Result<T>`** and **exceptions** for error handling in Domain-Driven Design (DDD). Pick the approach that best fits your project, your architecture, and your tolerance for boilerplate.

In general, prefer **`Result<T>`** for business operations that can fail in expected ways. This keeps failures explicit, avoids control flow via exceptions, and makes error handling visible instead of *somewhere up the call stack*.

Exceptions still have their place. APIs, for example, often bubble errors straight to the top anyway, where they're translated into HTTP responses. In those cases, throwing an exception can be the simpler and more honest approach without passing `Result<T>` throught twelve layers of services.

``` csharp
// Build errors fluently
var error = new ErrorBuilder()
    .WithCode("InvalidInvoiceNumber")
    .WithMessage("Invoice number must be after year 2000")
    .WithDetail("Year", year.ToString())
    // More details!: .WithDetail("Status", "caffeine overdose")
    .Build();

// Convert to exceptions when you want to
throw error.ToBusinessRuleException();
```

Integrates well with `FluentValidation`, because writing the same validation logic twice is a crime:

``` csharp
// Error object:
if (new YourFluentValidator().Validate(someObject).TryGetError(out var error))
{
    return error;
}

// Business exception:
new YourFluentValidator().ValidateAndThrowBusiness(result);
```

Plenty of extension methods are provided for converting between errors, exceptions, and `Result<T>` types, so you can stay consistent without reinventing error plumbing every sprint.

Comes with [`WellKnownErrors`](./Errors/WellKnown/ErrorCodes.cs) for common cases. Feel free to ignore them and make your own (I won't tell).

## Mediator - CQRS Without the Ceremony

Simple mediator pattern for Commands and Queries. No magic, just **delegates work to handlers via DI**.

Define a command:

``` csharp
public record CreateInvoiceCommand(/*params...*/) : ICommand<InvoiceId>;
```

Handle it:


``` csharp
public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(
        CreateInvoiceCommand command, 
        CancellationToken cancellationToken)
    {
        // Code
    }
}
```

Register it:

``` csharp
builder.Services.AddMediator(config =>
{
    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(integration);
    // ...
});
```

Send it:

``` csharp
var invoiceId = await mediator.SendCommand<CreateInvoiceCommand, InvoiceId>(
    new CreateInvoiceCommand(/*params*/), 
    cancellationToken
);
```

**Pipelines** – we got them! And they are **type-safe and nice**! **Pick who uses what pipelines**! Add cross-cutting concerns like logging, validation, transactions, etc. without cluttering your handlers.

*Using pipelines is super nice; but, to be honest, defining new pipelines requires a bit of boilerplate. Guys, it's a sacrifice for the type-safety, ok? In my defence, how often will you create new pipes huh?*

Check out the [demo app](../MartinDrozdik.DDD.Demo) for examples of pipelines in action:

``` csharp
builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();
    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(integration);
    // ...
});
```
