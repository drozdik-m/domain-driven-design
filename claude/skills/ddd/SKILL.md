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
- **Prefer** a result type for expected domain failures; throw exceptions at API boundaries where middleware translates them to HTTP responses.
- **Use** FluentValidation at system boundaries (input DTOs); use Specifications for named, reusable domain rules on domain objects.
- **Never** hand-write equality on a `ValueObject` subclass — the base already provides all of it. See [Value Object equality](#value-object-equality-is-already-implemented).

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

It is not either/or at the boundary: keep the `Enumeration` in the domain and declare a plain `enum` for the public API contract, then map between them with `ToStructEnum` / `FromStructEnum` — see [Exposing an enumeration through an API](#exposing-an-enumeration-through-an-api).

### Specifications vs FluentValidation

**FluentValidation** — input validation at system boundaries (API request DTOs): format, type, nullability, range.

**Specifications** — business rules inside the domain layer, when:
- The rule has a meaningful reusable name (`IsDraftSpecification`)
- The rule must explain *why* it failed, not just *that* it failed
- Rules are composed from simpler rules

Never use Specifications for null checks or format validation — that is FluentValidation's job. Keep specifications focused on complex, reusable domain rules.

### Results vs Exceptions

> **This library does not define its own `Result<T>`.** Results come from
> [CSharpFunctionalExtensions](https://www.nuget.org/packages/CSharpFunctionalExtensions) — `Result<T, E>`,
> `UnitResult<E>`, `IResult<T, E>` — which `MartinDrozdik.DDD` takes as a package dependency and pairs with
> its own `Error` type, so the shape you will almost always want is `Result<T, Error>` / `UnitResult<Error>`.
> The library's only bridge into it is `ErrorBuilder.BuildUnitResult()`. Do not go looking for a
> `MartinDrozdik.DDD.Errors.Result<T>` — there isn't one.

**`Result<T, Error>`** — when failure is an expected, valid business outcome; forces callers to handle both paths explicitly.

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

#### Value Object equality is already implemented

**Overriding `GetEqualityComponents()` is the entire contract.** `ValueObject` derives everything else from
it, so a subclass gets working value equality for free and must not add any of it by hand:

| Member | Where it comes from | Do you write it? |
|---|---|---|
| `operator ==` / `operator !=` | declared on `ValueObject`, null-safe | **No** — redeclaring on a subclass hides the base operators |
| `Equals(object?)` | overridden on `ValueObject` | **No** |
| `GetHashCode()` | overridden on `ValueObject`, folds each component with `HashCode.Combine` | **No** |
| `Equals(ValueObject?, ValueObject?)`, `GetHashCode(ValueObject)` | `ValueObject : IEqualityComparer<ValueObject>` | **No** |

Behaviour worth knowing, so you never have to go read the base class:

- Components are compared with `SequenceEqual`, so **order matters** — always `yield return` in a stable order.
- A `null` component is legal and hashes as `0`.
- `ValueObject` implements `IEqualityComparer<ValueObject>` and `IEqualityOperators<ValueObject, ValueObject, bool>`.
  It does **not** implement `IEquatable<T>`, and a subclass does not get `IEqualityOperators<TSelf, TSelf, bool>`.
  This matters when picking an `EqualityAssert` overload in tests — see the `ddd-testing` skill.
- There is **no runtime type check**: two different `ValueObject` subclasses whose components happen to match
  compare equal. Add a discriminator component if that is a real risk for your type.

Everything that derives from `ValueObject` inherits this: `Identity<TSelf, TKey>` (and so every strongly-typed
ID), `Enumeration`, `Error`, and `ErrorCode`.

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

Both markers are detectable at runtime, which is how infrastructure code applies conventions per role
(`MartinDrozdik.DDD.Templates.TypeExtensions`):

```csharp
typeof(Invoice).IsAggregateRoot();  // true
typeof(Person).IsDomainEntity();    // true
```

### Strongly-Typed Identities

```csharp
public class PersonId(Guid key)   : GuidIdentity<PersonId>(key);
public class OrderId(int key)     : IntIdentity<OrderId>(key);
public class SkuId(string key)    : StringIdentity<SkuId>(key);
```

EF Core mapping. The library ships `IdentityConverter<TIdentity, TKey>` plus the
`IdentityConverter.CreateGuid<T>(fromKey)` factory:

```csharp
builder.Property(e => e.Id)
    .HasIdentityConvertor(IdentityConverter.CreateGuid(key => new InvoiceId(key)));
```

> `HasIdentityConvertor` is **not** library API — it is a small `PropertyBuilder<T>` extension each app writes
> once. Copy it from the demo (`src/MartinDrozdik.DDD.Demo/Context/EntityFrameworkExtensions.cs`) before use.

For `int`/`string` keys there is no factory; construct the converter with both expressions:

```csharp
new IdentityConverter<OrderId, int>(id => id.Key, key => new OrderId(key));
```

### Enumeration

Pick the base by what the type needs to do:

| Base | Gives you | Use when |
|---|---|---|
| `Enumeration` | `Name`, value equality, implicit `string` conversion | A closed set you only reference by static member |
| `StaticEnumeration<TSelf>` | `+ FromName`, `FromNameOptional`, `GetAll()`, `FromStructEnum`, `FromStructEnumOptional` | You deserialize from a string or a .NET enum, or enumerate all members |
| `InitializableEnumeration<TSelf>` | same lookups, but the member set is supplied at startup via `Initialize(values)` | Members come from config or the database |

Each capability is declared by its own interface, so generic code can require exactly what it needs:
`IEnumerationDeserializer<TSelf>` (`FromName` / `FromNameOptional`), `IEnumerationEnumerator<TSelf>` (`GetAll`)
and `IStructEnumDeserializer<TSelf>` (`FromStructEnum` / `FromStructEnumOptional`). Both bases implement all three.

```csharp
public class InvoiceState(EnumerationName name) : Enumeration(name)
{
    public static InvoiceState Draft  => new(new EnumerationName("Draft"));
    public static InvoiceState Issued => new(new EnumerationName("Issued"));
    public static InvoiceState Paid   => new(new EnumerationName("Paid"));

    public bool CanBeModified() => this == Draft;
}
```

#### Enumeration equality is already implemented

`Enumeration : ValueObject`, so **every enumeration gets value equality for free** and must never declare its own
`==`, `Equals`, or `GetHashCode`. See [Value Object equality](#value-object-equality-is-already-implemented) for
the full contract; the enumeration-specific part is what goes into it:

**`GetEqualityComponents()` yields `Name` and nothing else.** Three consequences, all verified:

- **Reference identity is irrelevant.** `this == Draft` in `CanBeModified` works even though the property returns a
  fresh instance on every call. Members do not need to be singletons.
- **Only the name is compared.** Two instances with the same `Name` are equal even when their other properties
  differ, so a display label or metadata property is invisible to equality. That is usually what you want — but do
  not use equality to detect that two members carry different payloads.
- **There is no type check, and comparing two unrelated enumeration types is not a compile error.**
  `InvoiceState.Draft == OrderState.Draft` compiles and returns `true`, because the base `==` takes
  `(ValueObject?, ValueObject?)` and both sides yield the name `"Draft"`. Nothing warns you. Keep member names
  distinct across enumerations that meet in the same code, or compare a strongly-typed property instead.

`EnumerationName` is a `readonly struct` with its own `IEquatable<EnumerationName>` and `==`, and it compares
**case-sensitively** on `Key` — `"Draft"` and `"draft"` are different members. (It also exposes `KeyLowercase`,
but equality does not use it.)

Because an enumeration is a `ValueObject`, it does **not** implement `IEquatable<TSelf>`, which decides the
`EqualityAssert` overload you can use in tests — see the `ddd-testing` skill.

`StaticEnumeration<TSelf>` discovers its members by reflecting over the **public static fields** declared on
`TSelf`, so declare them as fields, not properties:

```csharp
public class InvoiceState(EnumerationName name) : StaticEnumeration<InvoiceState>(name)
{
    public static readonly InvoiceState Draft = new("Draft");
    public static readonly InvoiceState Issued = new("Issued");
    public static readonly InvoiceState Paid = new("Paid");
}

// IResult<InvoiceState, Error> — fails with EnumerationErrors.EnumerationNameNotFound
var state = InvoiceState.FromName("Draft");
var all = InvoiceState.GetAll();
```

EF Core mapping for an enumeration is a plain `HasConversion` over `Name.Key`:

```csharp
builder.Property(i => i.State)
    .HasConversion(e => e.Name.Key, e => new InvoiceState(e));
```

#### Exposing an enumeration through an API

An `Enumeration` is a domain type and should not appear on the public API surface. Declare a plain `enum` for
the contract and map between the two — do **not** hand-write a mapping method:

```csharp
using MartinDrozdik.DDD.Enumerations.Attributes;   // only for [EnumerationName]

public enum InvoiceStateDto
{
    Draft,
    Issued,

    // only when the names must differ
    [EnumerationName("Paid")]
    Settled,
}

// domain -> API
State = invoice.State.ToStructEnum<InvoiceStateDto>(),

// API -> domain (mirrors FromName)
var state = InvoiceState.FromStructEnum(dto.State);
```

Rules that matter:

- Matching is **by name, case-sensitively** — `EnumerationName.Key` against the .NET enum member name, unless
  `[EnumerationName]` overrides it. The attribute lives in `MartinDrozdik.DDD.Enumerations.Attributes`, so it needs
  its own `using` — the shorthand shares a name with the `EnumerationName` struct and will not resolve without it.
- `ToStructEnumOptional` and `FromStructEnumOptional` are the `null`-in/`null`-out siblings of each direction.
- A member with no counterpart **throws `BusinessRuleException`**, unlike `FromName` which returns a failed
  `IResult`. A broken mapping contract is a bug, not a business failure. Rule it out up front with
  `EnumerationStructMapping.ThrowIfIncomplete<InvoiceState, InvoiceStateDto>()` — it fails when either side has a
  member the other lacks, and belongs at startup or in a test. An `InitializableEnumeration<TSelf>` must be
  initialized before the call, because the check lists its members. In an xUnit project, derive from
  `EnumerationStructMappingTests<InvoiceState, InvoiceStateDto>` (`MartinDrozdik.DDD.Testing`) instead — a one-line
  class declaration with an empty body covers the bijection *and* both round trips.
- `FromStructEnum` goes through `FromName`, so the domain side must be a
  `StaticEnumeration<TSelf>` or an `InitializableEnumeration<TSelf>` — a plain `Enumeration` has no lookup. Generic
  code can require the capability with `where T : Enumeration, IStructEnumDeserializer<T>`.
- `[Flags]` enums and value aliases (`A = 1, B = 1`) are rejected with `ArgumentException`: a flag combination has
  no single name, and an alias makes the mapping ambiguous.

For values arriving off the wire, model binding produces undefined members (`?state=99` → `(InvoiceStateDto)99`).
Those map to nothing, so turn them into a validation failure rather than a 500:

```csharp
RuleFor(x => x.State).MustMapToEnumeration(EnumerationMap.To<InvoiceState>());
```

`EnumerationMap.To<T>()` exists only so every generic argument can be inferred — C# infers all or nothing, and
without it the call site would have to spell out the validated type, the .NET enum and the enumeration.

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

`ErrorBuilder` also collapses child errors into details, and can terminate straight into a failed result
instead of an exception:

```csharp
var error = new ErrorBuilder()
    .WithCode("InvoiceInvalid")
    .WithMessage("Invoice could not be issued.")
    .WithSubErrors(lineErrors)   // flattens each sub-error's code, message and details
    .WithCause(innerException)
    .Build();

// UnitResult<Error> failure, no exception thrown
return new ErrorBuilder()
    .WithCode("InvoiceInvalid")
    .WithMessage("Invoice could not be issued.")
    .BuildUnitResult();
```

`Build()` throws if either the code or the message is missing — both are required.

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

Registration. Note `LoggingPipelineIntegrator` ships in **MartinDrozdik.DDD.Web**, not core —
`ValidationPipelineIntegrator` is the only integrator in this package:

```csharp
builder.Services.AddMediator(config =>
{
    var pipeline = new LoggingPipelineIntegrator()   // MartinDrozdik.DDD.Web
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
