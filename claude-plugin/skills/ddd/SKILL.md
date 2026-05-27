---
description: Use when implementing DDD building blocks with MartinDrozdik.DDD — ValueObject, Entity, AggregateRoot, strongly-typed IDs, Enumerations, Specifications, error handling with ErrorBuilder/Result<T>, or the CQRS Mediator with Commands, Queries, and Pipelines.
---

You are an expert in the **MartinDrozdik.DDD** library. Help the user implement correct DDD patterns using this specific library.

## Library philosophy

- **Pragmatic over purist** — use what makes sense, ignore what doesn't
- **Composition over inheritance** — interfaces, not deep abstract hierarchies
- **Explicit over implicit** — code should be obvious without explanation
- **Type safety** — compiler errors beat runtime errors; no primitive obsession
- **Fail fast** — validation and error handling built in from the start

Install: `dotnet add package MartinDrozdik.DDD`

## User request

$ARGUMENTS

---

## When to use which construct

### Value Object vs Entity

Use a **Value Object** when the concept has no meaningful identity — two instances with the same data are interchangeable. They are immutable and compared by their attributes. Examples: `Money`, `Address`, `InvoiceNumber`, `DateRange`, `PhoneNumber`.

Use an **Entity** when the concept has an identity that persists over time even as its attributes change. A `Person` is still the same person after a name change. Entities are tracked by their ID, not their data.

Decision question: *"If I swap this instance for another with identical data, does it matter?"* If no → Value Object. If yes → Entity.

### Entity vs Aggregate Root

Use an **Entity** for concepts that belong inside an aggregate and are only accessible through the aggregate root. They cannot be loaded or saved independently.

Use an **Aggregate Root** for concepts that are:
- The consistency boundary (enforce all invariants for entities inside them)
- The unit of persistence (but one aggregate can span multiple tables if needed)
- Accessible directly from application code

Decision question: *"Can this concept exist and be loaded independently, or does it only make sense as part of something else?"* Standalone → Aggregate Root. Subordinate → Entity. Ask the user to clarify the business case.

### Enumeration vs enum

Use a plain `enum` when: values are purely numeric flags, there is no behavior, no extra properties, and no string serialization needed.

Use `Enumeration` when any of the following apply:
- The values need to serialize to strings in the database or JSON
- The values carry behavior (methods like `CanBeModified()`)
- The values have extra display or metadata properties
- You need to add new values without changing switch statements everywhere

### Specifications vs FluentValidation

Use **FluentValidation** at system boundaries: validating incoming API request DTOs, checking data format, type, and range. This is input sanitization — keep it at the edge.

Use **Specifications** inside the domain for business rules that:
- Have a meaningful name worth reusing (`IsDraftSpecification`, `CustomerIsEligibleForDiscount`)
- Need to explain *why* they failed, not just *that* they failed
- Are composed from simpler rules
- Apply to domain objects, not raw input DTOs
- Only use for complex reusable rules, try to keep is simple

Never use Specifications for simple null or format checks — that is FluentValidation's job.

### Result\<T\> vs Exceptions

Use **`Result<T>`** when failure is an expected, normal outcome of a business operation. It forces the caller to handle both paths and keeps failures explicit in the return type.

Use **exceptions** when:
- The failure is truly unexpected (programming error)
- You are at an API handler boundary where exceptions bubble to the error middleware anyway
- Passing `Result<T>` through many layers would add noise without value

Exception → HTTP status mapping (when using `MartinDrozdik.DDD.Web`):

| Exception | HTTP status | When to throw |
|---|---|---|
| `BusinessRuleValidationException` | 400 Bad Request | User input violated a business rule |
| `ValidationException` (FluentValidation) | 400 Bad Request | Input failed validation |
| `BusinessNotFoundException` | **404 Not Found** | A required resource does not exist |
| `BusinessRuleException` | 500 Internal Server Error | Unexpected business state violation |

Throw `BusinessNotFoundException` (subclass of `BusinessRuleException`) when a lookup fails and the caller should receive 404:

```csharp
var invoice = await repository.FindAsync(id)
    ?? throw new BusinessNotFoundException($"Invoice {id} not found.");
```

### Mediator (CQRS) vs direct calls

Use the **Mediator** when:
- Commands or queries cross module or service boundaries
- You want pipeline behavior (logging, validation, transactions) applied uniformly across handlers

Use direct method/service calls when the operation is simple CRUD within a single module — mediator overhead is not justified there.

Commands mutate state; Queries only read it. Never mix the two in one handler.

---

## Building blocks — how to implement them correctly

### Value Objects

Compared by value, not identity. Override `GetEqualityComponents()`:

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
        return new InvoiceNumber(year, order);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Order;
    }
}
```

### Entities

Compared by identity. Implement `IDomainEntity<TIdentity>` (marker interface only — no base class):

```csharp
public class Person : IDomainEntity<PersonId>
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
        return new Person(new PersonId(Guid.CreateVersion7()), fullName);
    }
}
```

### Aggregate Roots

Same as Entity, just use `IAggregateRoot<TIdentity>` — marks consistency boundaries:

```csharp
public class Invoice : IAggregateRoot<InvoiceId> { ... }
```

### Strongly-Typed Identities

Never use naked `Guid`, `int`, or `string` for IDs:

```csharp
public class PersonId(Guid key) : GuidIdentity<PersonId>(key);
public class OrderId(int key) : IntIdentity<OrderId>(key);
public class SkuId(string key) : StringIdentity<SkuId>(key);
```

Use `Guid.CreateVersion7()` for new GUIDs (not `Guid.NewGuid()`).

EF Core mapping:

```csharp
builder.Property(e => e.Id)
    .HasIdentityConvertor(new IdentityConverter<InvoiceId, Guid>());
```

### Enumerations

When `enum` isn't enough (you need behavior, extra properties, string serialization):

```csharp
public class InvoiceState(EnumerationName name) : Enumeration(name)
{
    public static InvoiceState Draft => new(new EnumerationName("Draft"));
    public static InvoiceState Issued => new(new EnumerationName("Issued"));
    public static InvoiceState Paid => new(new EnumerationName("Paid"));

    public bool CanBeModified() => this == Draft;
}
```

Compares by value, serializes to string in DB/JSON, extends like a class.

### Specifications

Encapsulate business rules as composable, named objects that explain *why* they fail:

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

`SpecificationResult` implicitly converts to `bool`. Three usage paths:

```csharp
// Simple boolean check
if (!spec.IsSatisfiedBy(context)) return;

// Rich errors
var result = spec.IsSatisfiedBy(context);
if (!result) return result.Errors; // IReadOnlyList<Error>

// Throw on failure
if (!spec.TrySatisfyBy(this, out var specResult))
{
    throw new ErrorBuilder()
        .WithCode("CannotChangeIssuer")
        .WithMessage("The issuer cannot be changed.")
        .WithSpecificationResult(specResult)
        .BuildValidationException();
}
```

Composition — operators map to their boolean counterparts:

```csharp
// Fluent
var spec = new OrderTotalGreaterThan(100)
    .And(new CustomerIsVip())
    .Or(new CustomerActiveYears(5));

// & / | are greedy (collect errors from both sides)
// && / || short-circuit when outcome is deterministic
```

Negation with a custom error:

```csharp
var notVip = new CustomerIsVip().Not(new ErrorBuilder()
    .WithCode("CustomerIsVip")
    .WithMessage("VIP customers are not eligible.")
    .Build());
```

Always-true / always-false guards:

```csharp
var permissive = TautologySpecification<OrderContext>.Instance;
var disabled = new ContradictionSpecification<OrderContext>(someError);
```

### Error Handling

Prefer `Result<T>` for expected business failures; throw exceptions when bubbling to HTTP handlers.

Build errors fluently:

```csharp
var error = new ErrorBuilder()
    .WithCode("InvalidInvoiceNumber")
    .WithMessage("Invoice number must be after year 2000")
    .WithDetail("Year", year.ToString())
    .Build();

// Convert to exceptions
throw error.ToBusinessRuleException();
```

When a resource is not found, throw `BusinessNotFoundException` — it is a subclass of `BusinessRuleException` and the web middleware maps it to **404 Not Found**:

```csharp
var invoice = await repository.FindAsync(id)
    ?? throw new BusinessNotFoundException($"Invoice {id} not found.");
```

FluentValidation integration:

```csharp
// Return error from validation
if (new YourFluentValidator().Validate(obj).TryGetError(out var error))
    return error;

// Throw business exception
new YourFluentValidator().ValidateAndThrowBusiness(result);
```

### Mediator (CQRS)

Define commands (state mutations) and queries (reads) as records:

```csharp
// Command
public record CreateInvoiceCommand(string CustomerName, decimal Total) : ICommand<InvoiceId>;

// Query
public record GetInvoiceQuery(InvoiceId Id) : IQuery<Invoice>;
```

Handlers:

```csharp
public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // implementation
    }
}
```

Registration with pipelines:

```csharp
builder.Services.AddMediator(config =>
{
    var integration = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();

    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(integration);
    config.WithQuery<GetInvoiceQuery, Invoice, GetInvoiceQueryHandler>(integration);
});
```

Dispatch:

```csharp
var invoiceId = await mediator.SendCommand<CreateInvoiceCommand, InvoiceId>(
    new CreateInvoiceCommand(customerName, total),
    cancellationToken);

var invoice = await mediator.SendQuery<GetInvoiceQuery, Invoice>(
    new GetInvoiceQuery(id),
    cancellationToken);
```

## Reference

See the [demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) for full working examples — Models/, Requests/, Context/.
