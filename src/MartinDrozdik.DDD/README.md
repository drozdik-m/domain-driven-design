# Flexible Domain-Driven Design (DDD) library for .NET

A pragmatic .NET library for Domain-Driven Design that doesn't force you into abstract nonsense (that much). Check the [demo app](../MartinDrozdik.DDD) for recommended usage.

## Installation

```bash
dotnet add package MartinDrozdik.DDD
```

## Philosophy

- **Pragmatic over purist** - Use what makes sense, ignore what doesn't
- **Type safety** - Compiler errors > Runtime errors (screw reflection)
- **Explicit over implicit** - Code should be obvious
- **Composition over inheritance** - Most templates are interfaces for a reason
- **Fail fast** - Validation at boundaries, not 67 layers deep

*Use the parts that help. Ignore the rest. Good luck.*

## Demo Project

The demo project shows recommended patterns. It's not gospel, but it works. Check out:

- [Models/](../MartinDrozdik.DDD.Demo/Models) - Aggregates, Entities, Value Objects, Enumerations
- [Requests/](../MartinDrozdik.DDD.Demo/Requests) - Commands and Queries with handlers
- [Context/](../MartinDrozdik.DDD.Demo/Context) - EF Core configuration with identity converters

## Templates (keeping it simple, stupid)

**Basic interfaces that define DDD building blocks.** These are interfaces, not abstract classes, because your ORM probably needs that flexibility anyway.

Check out the [demo app](../MartinDrozdik.DDD) for more examples with validation and other goodies:

- [Person entity with validation and strongly typed ID](../MartinDrozdik.DDD.Demo/Models/Entities/Person.cs)
- [Invoice aggregate with validation and strongly typed ID](../MartinDrozdik.DDD.Demo/Models/Aggregates/Invoice.cs)
- [Value Object with validation for an Invoice Number](../MartinDrozdik.DDD.Demo/Models/ValueObjects/InvoiceNumber.cs)
- [In-Memory state enumeration](../MartinDrozdik.DDD.Demo/Models/Enumerations/InvoiceState.cs)

### Value Objects

Things compared by value, not identity:

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

Things compared by identity, not value. Just implement the marker interface and you're good to go:

```csharp
public class Person : IDomainEntity<Guid> // or IAggregateRoot<Guid>
{
    private Person(Guid id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public Guid Id { get; }
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

Strongly-Typed IDs (No More `Guid` Soup).

Stop passing around naked `Guid`s and `int`s like it's 2010. Type-safe identities with implicit conversion support.

```csharp
public class PersonId(Guid key) : GuidIdentity<PersonId>(key);

// Usage:
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

Serializes to strings/properties in your DB/JSON. Compares by value. Extends like a class. It's beautiful 😍.

## Exceptions and Errors

*Stuff happens, and you need to handle it.*

This library provides support for both **Result and Exception** handling strategies in Domain-Driven Design (DDD). You can choose the approach that best fits your project's needs.

Normally, you would use `Result<T>` types to represent the outcome of business operations that can fail, allowing you to handle errors in a functional way without throwing exceptions. This is particularly useful in scenarios where you want to avoid the overhead of exceptions and prefer to work with explicit success/failure states.

However, applications like APIs usually propagate the error all the way to the top level anyway, where exceptions can be caught and translated into appropriate HTTP responses. In such cases, using exceptions might be more straightforward without tons of boilerplate.

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

Integrates well with `FluentValidation`:

``` csharp
// Error object:
if (new YourFluentValidator().Validate(someObject).TryGetError(out var error))
{
    return error;
}

// Business exception:
new YourFluentValidator().ValidateAndThrowBusiness(result);
```

Many more extension methods for converting between errors and exceptions, and for working with `Result<T>` types.

Comes with [`WellKnownErrors`](./Errors/WellKnown/ErrorCodes.cs) for common cases. Feel free to ignore them and make your own.

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

Pipelines – we got them! And they are **type-safe and nice**! **Pick who uses what pipelines**! Add cross-cutting concerns like logging, validation, transactions, etc. without cluttering your handlers.

*Using pipelines is super nice; but, to be honest, defining new pipelines requires a bit of boilerplate. It's a sacrifice for the type-safety guys...*

Check out the [demo app](../MartinDrozdik.DDD) for examples of pipelines in action:

``` csharp
builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();
    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(integration);
    // ...
});
```
