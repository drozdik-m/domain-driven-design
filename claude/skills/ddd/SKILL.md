---
description: Use when implementing DDD building blocks with MartinDrozdik.DDD — ValueObject, Entity, AggregateRoot, strongly-typed IDs, Enumerations, Specifications, error handling with ErrorBuilder/Result<T>, or the CQRS Mediator with Commands, Queries, and Pipelines.
---

You are an expert in the **MartinDrozdik.DDD** library. Generate correct, idiomatic code using its specific APIs and patterns.

## Request

$ARGUMENTS

If the domain model is ambiguous (unclear whether something is an Entity or Value Object, what the aggregate boundary is, etc.) ask one focused question before generating code.

---

## Rules

- **Use** naked `Guid`, `int`, or `string` for entity/aggregate IDs or typed identity wrappers depending on current project practices.
- **Never** use base classes where interfaces suffice — `IDomainEntity`, `IAggregateRoot` are marker interfaces, not base classes.
- **Never** use `Guid.NewGuid()` — use `Guid.CreateVersion7()`.
- **Prefer** `Result<T>` for expected domain failures; throw exceptions at API boundaries where middleware translates them to HTTP responses.
- **Use** FluentValidation at system boundaries (input DTOs); use Specifications for named, reusable domain rules on domain objects.

Install: `dotnet add package MartinDrozdik.DDD`

---

## When to use which construct

### Value Object vs Entity

| | Value Object | Entity |
|---|---|---|
| Identity | None — defined by its attributes | Persistent ID, independent of attributes |
| Equality | By value (all attributes equal) | By ID |
| Mutability | Immutable | Mutable over its lifetime |
| Examples | `Money`, `Address`, `InvoiceNumber`, `DateRange` | `Person`, `Order`, `Product` |

**Decision test:** *"If I replace this instance with another that has identical data, does it matter?"* No → Value Object. Yes → Entity.

### Entity vs Aggregate Root

An **Entity** belongs inside an aggregate; it is only accessible through its root and cannot be loaded or saved independently.

An **Aggregate Root** is:
- The consistency boundary — enforces all invariants for the entities it contains
- The unit of persistence — one `DbSet<T>` per aggregate root
- Directly accessible from application code

**Decision test:** *"Can this concept be loaded and manipulated independently, or does it only make sense as part of something else?"* Standalone → Aggregate Root. Subordinate → Entity.

### Enumeration vs enum

Use `enum` when: purely numeric flags, no behavior, no extra properties, no string serialization.

Use `Enumeration` when any of the following apply:
- Must serialize to strings in DB or JSON
- Carries behavior (methods like `CanBeModified()`)
- Has extra display or metadata properties
- Needs to be extended without touching switch statements

### Specifications vs FluentValidation

**FluentValidation** — input validation at system boundaries (API request DTOs): format, type, nullability, range.

**Specifications** — business rules inside the domain layer, when:
- The rule has a meaningful reusable name (`IsDraftSpecification`)
- The rule must explain *why* it failed, not just *that* it failed
- Rules are composed from simpler rules

Never use Specifications for null checks or format validation — that is FluentValidation's job. Keep specifications focused on complex, reusable domain rules.

### Result\<T\> vs Exceptions

**`Result<T>`** — when failure is an expected, valid business outcome; forces callers to handle both paths explicitly.

**Exceptions** — when the failure is truly unexpected, or at an API handler boundary where exceptions bubble to error middleware anyway.

Exception → HTTP status (via `MartinDrozdik.DDD.Web`):

| Exception | HTTP status | When to throw |
|---|---|---|
| `BusinessRuleValidationException` | 400 Bad Request | User input violated a business rule |
| `ValidationException` (FluentValidation) | 400 Bad Request | Input failed validation |
| `BusinessNotFoundException` | **404 Not Found** | A required resource does not exist |
| `BusinessRuleException` | 500 Internal Server Error | Unexpected business state violation |

### Mediator vs direct calls

Use the **Mediator** when commands/queries cross module boundaries or when you need pipeline behavior (logging, validation, transactions) applied uniformly.

Use direct service calls within a single, self-contained module where mediator overhead adds no value.

**Commands** mutate state. **Queries** only read. Never mix them in one handler.

---

## Implementation reference

### Value Object

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
        => new(year, order);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Year;
        yield return Order;
    }
}
```

### Entity

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
        => new(new PersonId(Guid.CreateVersion7()), fullName);
}
```

### Aggregate Root

Same shape as Entity — only the interface changes:

```csharp
public class Invoice : IAggregateRoot<InvoiceId> { ... }
```

### Strongly-Typed Identities

```csharp
public class PersonId(Guid key)   : GuidIdentity<PersonId>(key);
public class OrderId(int key)     : IntIdentity<OrderId>(key);
public class SkuId(string key)    : StringIdentity<SkuId>(key);
```

EF Core mapping:

```csharp
builder.Property(e => e.Id)
    .HasIdentityConvertor(new IdentityConverter<InvoiceId, Guid>());
```

### Enumeration

```csharp
public class InvoiceState(EnumerationName name) : Enumeration(name)
{
    public static InvoiceState Draft  => new(new EnumerationName("Draft"));
    public static InvoiceState Issued => new(new EnumerationName("Issued"));
    public static InvoiceState Paid   => new(new EnumerationName("Paid"));

    public bool CanBeModified() => this == Draft;
}
```

### Specifications

```csharp
private class IsDraftSpecification : ISpecification<Invoice>
{
    public SpecificationResult IsSatisfiedBy(Invoice invoice)
    {
        if (invoice.State != InvoiceState.Draft)
            return new ErrorBuilder()
                .WithCode("InvoiceMustBeDraft")
                .WithMessage($"Invoice must be in {InvoiceState.Draft} state.")
                .Build();

        return SpecificationResult.Satisfied;
    }
}
```

`SpecificationResult` implicitly converts to `bool`. Three evaluation paths:

```csharp
// 1. Boolean only
if (!spec.IsSatisfiedBy(context)) return;

// 2. Rich errors
var result = spec.IsSatisfiedBy(context);
if (!result) return result.Errors; // IReadOnlyList<Error>

// 3. Throw on failure
if (!spec.TrySatisfyBy(this, out var specResult))
    throw new ErrorBuilder()
        .WithCode("CannotChangeIssuer")
        .WithMessage("The issuer cannot be changed.")
        .WithSpecificationResult(specResult)
        .BuildValidationException();
```

Composition:

```csharp
var spec = new OrderTotalGreaterThan(100)
    .And(new CustomerIsVip())
    .Or(new CustomerActiveYears(5));

// & / |  — greedy: collect errors from both sides
// && / || — short-circuit when outcome is deterministic
```

```csharp
// Negation with a custom error
var notVip = new CustomerIsVip().Not(new ErrorBuilder()
    .WithCode("CustomerIsVip")
    .WithMessage("VIP customers are not eligible.")
    .Build());

// Always-true / always-false guards
var permissive = TautologySpecification<OrderContext>.Instance;
var disabled   = new ContradictionSpecification<OrderContext>(someError);
```

### Error Handling

```csharp
var error = new ErrorBuilder()
    .WithCode("InvalidInvoiceNumber")
    .WithMessage("Invoice number must be after year 2000")
    .WithDetail("Year", year.ToString())
    .Build();

throw error.ToBusinessRuleException();
```

`BusinessNotFoundException` is a subclass of `BusinessRuleException`; throw it when a lookup yields nothing — the web middleware maps it to **404**:

```csharp
var invoice = await repository.FindAsync(id)
    ?? throw new BusinessNotFoundException($"Invoice {id} not found.");
```

FluentValidation integration:

```csharp
if (new YourValidator().Validate(obj).TryGetError(out var error))
    return error;

new YourValidator().ValidateAndThrowBusiness(obj);
```

### Mediator (CQRS)

```csharp
public record CreateInvoiceCommand(string CustomerName, decimal Total) : ICommand<InvoiceId>;
public record GetInvoiceQuery(InvoiceId Id) : IQuery<Invoice>;
```

```csharp
public class CreateInvoiceCommandHandler : ICommandHandler<CreateInvoiceCommand, InvoiceId>
{
    public async Task<InvoiceId> HandleAsync(
        CreateInvoiceCommand command, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

```csharp
// Registration
builder.Services.AddMediator(config =>
{
    var pipeline = new LoggingPipelineIntegrator()
        .Merge<ValidationPipelineIntegrator>();

    config.WithCommand<CreateInvoiceCommand, InvoiceId, CreateInvoiceCommandHandler>(pipeline);
    config.WithQuery<GetInvoiceQuery, Invoice, GetInvoiceQueryHandler>(pipeline);
});

// Dispatch
var invoiceId = await mediator.SendCommand<CreateInvoiceCommand, InvoiceId>(
    new CreateInvoiceCommand(customerName, total), cancellationToken);

var invoice = await mediator.SendQuery<GetInvoiceQuery, Invoice>(
    new GetInvoiceQuery(id), cancellationToken);
```

## Reference

[Demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) — Models/, Requests/, Context/ for full working examples.
