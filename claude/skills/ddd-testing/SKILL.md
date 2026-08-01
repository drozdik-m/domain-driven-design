---
description: Use when writing tests with MartinDrozdik.DDD.Testing — TestedApp, TestedAppBuilder, smoke tests (WebApplicationSmokeTests, OpenApiSmokeTests, ErrorHandlingTests, RecurringTaskSmokeTests, EndpointSmokeTester), EF Core integration tests with SqlDbContextIntegrationTests, TestLogger for asserting on log output, EqualityAssert for value objects, or ResultAssert for results.
---

You are an expert in the **MartinDrozdik.DDD.Testing** library. Generate correct integration test infrastructure and test code using its specific APIs.

## Request

$ARGUMENTS

---

## Rules

- **Prefer integration tests** — test the whole stack with real dependencies; they reveal far more bugs than isolated unit tests.
- **Always call `Dispose()`** on `TestedApp<T>` — implement `IDisposable` in every test class that holds one.
    - Prefer direct build inside test cases: `using var app = new MyAppBuilder(output).Build();` to ensure proper disposal even if exceptions occur.
- Tests run in the **"Testing" environment** by default. Keep test config in `appsettings.Testing.json`, separate from development config.
- Test method names are `snake_case` sentences; test classes end in `Tests`.

Install:
```bash
dotnet add package xunit.v3
dotnet add package MartinDrozdik.DDD.Testing
```

---

## TestedAppBuilder — one-time setup per test project

Subclass once per project, referencing your ASP.NET Core `Program`:

```csharp
public class MyAppBuilder(ITestOutputHelper output) : TestedAppBuilder<Program>(output)
{
    // Add shared overrides that apply to every test in this project:
    // .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
}
```

## TestedApp — using the app in tests

```csharp
public class MyTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestedApp<Program> _app = new MyAppBuilder(output).Build();

    [Fact]
    public async Task Something_works()
    {
        var client = _app.CreateClient();
        var response = await client.GetAsync("/something");
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _app.Dispose();
}
```

When a test class needs different overrides from the shared builder, configure inline:

```csharp
public class MyIsolatedTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestedApp<Program> _app = new MyAppBuilder(output)
        .WithOption<SomeOptions>(o => o.Flag, "true")
        .WithServices(services => services.AddSingleton<IMyService, FakeMyService>())
        .WithEndpoints(endpoints => endpoints.MapGet("/test-only", () => "hello"))
        .Build();

    public void Dispose() => _app.Dispose();
}
```

## Full builder API

All `With*` calls are additive and stack in order:

```csharp
new MyAppBuilder(output)
    .With(builder => builder.UseSetting("Key", "Value"))       // raw IWebHostBuilder access
    .WithOption("App:Database:ConnectionString", "Data Source=:memory:")
    .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
    .WithServices(services => services.AddSingleton<IMyService, MockMyService>())
    .WithEndpoints(endpoints => endpoints.MapGet("/test-route", () => Results.Ok()))
    .WithDisposable(() => Console.WriteLine("cleaned up"))
    .WithFakeTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))  // or a FakeTimeProvider
    .WithTestingLogger(out var logs)                  // capture everything the app logs
    .WithEnvironment("Development")                   // override environment; "Testing" by default
    .WithUserAndRoles("testuser", ["Admin", "User"])  // fake authenticated user
    .WithClaimsPrincipal(myClaimsPrincipal)           // full claims control
    .WithRecurringTasks()                             // keep background loops (removed by default)
    .WithoutHostedService<MyWorker>()                 // drop one of your own hosted services
    .Build();
```

Two things to watch:


- **`WithOption` values are always `string`.** Both overloads take a `string value`, because they go through
  `IWebHostBuilder.UseSetting`. Pass `"true"`, not `true`.

`WithTestingLogger` registers a `TestLogger` capturing everything from application start, so a test can assert on
log output. The app logs at `Information` and above by default, so anything below that never arrives:

```csharp
Assert.Contains(logs.AtLeast(LogLevel.Warning), e => e.Message.Contains("retrying"));
logs.From<CleanupTask>();   // entries from one category
logs.Last;                  // most recent LogEntry
```

## Recurring Tasks

`Build()` **removes every recurring task loop by default**, so background work never fires mid-test. The tasks stay registered — only the loops are gone.

`RecurringTaskTestExtensions` has exactly one member, in two overloads — on `ITestedApp` and on `IServiceProvider`:

```csharp
// Run one iteration on demand, in a fresh scope, exactly as the loop would.
// A direct invocation, not the loop: it rethrows what the task throws and ignores Enabled and Timeout.
await app.RunRecurringTaskAsync<CleanupTask>(TestContext.Current.CancellationToken);
```

That is normally all a test needs. For the schedule or a trigger, resolve them yourself:

```csharp
var schedule = app.Services.GetRequiredService<IOptions<RecurringTaskOptions<CleanupTask>>>().Value;
app.Services.GetRequiredService<IRecurringTaskTrigger<CleanupTask>>().Trigger();
```

Triggering only does something when the loops are actually running, i.e. after `.WithRecurringTasks()`.

Free per-task wiring smoke tests — registration, resolution of all dependencies from a scope, and a schedule that passes the application's own options validation. Never executes the task:

```csharp
public class CleanupTaskSmokeTests(ITestOutputHelper output)
    : RecurringTaskSmokeTests<Program, CleanupTask>(new MyAppBuilder(output)) { }
```

## Smoke Tests

Inherit from the base classes — no test body needed:

```csharp
// Verifies the app starts and health endpoints respond
public class MyAppSmokeTests(ITestOutputHelper output)
    : WebApplicationSmokeTests<Program>(new MyAppBuilder(output)) { }

// Verifies OpenAPI documents are valid JSON/YAML
public class MyOpenApiSmokeTests(ITestOutputHelper output)
    : OpenApiSmokeTests<Program>(new MyAppBuilder(output))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints() =>
    [
        new OpenApiEndpoint("/openapi/v1.json", OpenApiType.Json),
        new OpenApiEndpoint("/openapi/v1.yaml", OpenApiType.Yaml),
    ];
}

// Verifies error handling middleware is wired correctly
public class MyAppErrorHandlingTests(ITestOutputHelper output)
    : ErrorHandlingTests<Program>(new MyAppBuilder(output)) { }
```

`GetOpenApiEndpoints` is the only member any of these *requires* you to override.

To smoke-test your own endpoints, drive `EndpointSmokeTester` from a `[Theory]`:

```csharp
public class EndpointTests(ITestOutputHelper output) : IDisposable
{
    private readonly EndpointSmokeTester<Program> _tester = new(new MyAppBuilder(output));

    public static TheoryData<EndpointTest> Endpoints =>
    [
        new EndpointTest(HttpMethod.Get, "/invoices"),
        new EndpointTest(HttpMethod.Get, "/invoices/me")
            .WithAcceptableCodes(HttpStatusCode.Unauthorized),   // protected endpoint
    ];

    [Theory]
    [MemberData(nameof(Endpoints))]
    public Task Endpoint_responds(EndpointTest endpoint)
        => _tester.Test(endpoint, TestContext.Current.CancellationToken);

    public void Dispose() => _tester.Dispose();
}
```

`EndpointTest` also carries `Content` and `ContentType` for request bodies. Set them with an object initializer —
`WithAcceptableCodes` returns a new instance and does **not** carry them over.

## EF Core Integration Tests

Verifies entity mappings, model compilation, migrations, and basic connectivity against a real database:

```csharp
public class MyDbContextTests(ITestOutputHelper output)
    : SqlDbContextIntegrationTests<MyDbContext>, IDisposable
{
    private readonly TestedApp<Program> _app =
        new MyAppBuilder(output)
            .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
            .Build();

    protected override MyDbContext GetContext()
        => _app.GetScopedService<MyDbContext>();

    // Optional: exclude entities that cannot be queried standalone (keyless, views, ...)
    protected override bool SkipEntityTest(string entityName)
        => entityName.EndsWith("Projection", StringComparison.Ordinal);

    public void Dispose() => _app.Dispose();
}
```

`GetContext()` is the only required override. The base class is not `IDisposable`, so the deriving class owns
disposing the app — as above.

## Calling the app

`CreateClient()` gives the raw `HttpClient`. For JSON round-trips, the `ITestedApp` extensions return a
`RequestResult`/`RequestResult<T>` instead, which bundles the response with the deserialized body:

```csharp
var result = await _app.GetJsonAsync<InvoiceDto>($"/invoices/{id}");
await result.EnsureSuccessAsync();       // fails the test with the response body on non-2xx
Assert.Equal(id, result.Value.Id);

await _app.PostJsonAsync("/invoices", newInvoice);                            // RequestResult
await _app.PostJsonWithResponseAsync<CreateInvoice, InvoiceDto>("/invoices", newInvoice);
await _app.PutJsonAsync($"/invoices/{id}", updated);
await _app.PutJsonWithResponseAsync<UpdateInvoice, InvoiceDto>($"/invoices/{id}", updated);
await _app.DeleteAsync($"/invoices/{id}");
await _app.DeleteJsonAsync<InvoiceDto>($"/invoices/{id}");
```

These pick up `TestContext.Current.CancellationToken` themselves, so no token argument is needed.

`GetScopedService<T>()` resolves a service from a scope the app tracks and disposes for you — use it instead of
`Services.GetRequiredService<T>()` for anything scoped, such as a `DbContext`.

## Assertions

### EqualityAssert

Covers symmetry, null/default comparisons, and hash code consistency in one call. In every method
`a` and `b` must be equal (same value, different instances) and `c` must be unequal.

**Pick the overload by which interfaces the type actually implements** — the constraints differ, and getting
this wrong is a compile error rather than a failing test:

| Method | Constraint on `T` |
|---|---|
| `TestAllEqualityBehaviors(a, b, c, comparer = null)` | `IEquatable<T>` **and** `IEqualityOperators<T, T, bool>` |
| `TestEquatable(a, b, c)` | `IEquatable<T>` |
| `TestEqualityOperators(a, b, c)` | `IEqualityOperators<T, T, bool>` |
| `TestEqualityComparer(comparer, a, b, c)` | none — **comparer comes first**, four arguments |

#### Value objects

`ValueObject` implements `IEqualityComparer<ValueObject>` and `IEqualityOperators<ValueObject, ValueObject, bool>`,
but **not** `IEquatable<T>`, and a subclass does not get `IEqualityOperators<TSelf, TSelf, bool>`. So
`TestAllEqualityBehaviors` and `TestEquatable` do not compile for a value object. Use these two, with the
value object itself as the comparer and `ValueObject` as the explicit type argument:

```csharp
[Fact]
public void InvoiceNumber_equality_is_correct()
{
    // Arrange
    var a = InvoiceNumber.Create(2024, 1);
    var b = InvoiceNumber.Create(2024, 1); // equal to a
    var c = InvoiceNumber.Create(2024, 2); // not equal to a

    // Act & Assert
    EqualityAssert.TestEqualityComparer(comparer: a, a, b, c);
    EqualityAssert.TestEqualityOperators<ValueObject>(a, b, c);
}
```

This is the pattern the library's own tests use — see `ValueObjectTests` and `PrimitiveIdentityTests`. It applies
to everything deriving from `ValueObject`, which includes **enumerations** (`Enumeration`, `StaticEnumeration<T>`,
`InitializableEnumeration<T>`), strongly-typed identities, `Error`, and `ErrorCode`:

```csharp
EqualityAssert.TestEqualityComparer(comparer: InvoiceState.Draft, InvoiceState.Draft, InvoiceState.Draft, InvoiceState.Paid);
EqualityAssert.TestEqualityOperators<ValueObject>(InvoiceState.Draft, InvoiceState.Draft, InvoiceState.Paid);
```

An enumeration compares on `Name` alone, so `a` and `b` can be any two members with the same name — including two
separate reads of the same static property, which return different instances.

`TestAllEqualityBehaviors` is for types that genuinely implement both interfaces, such as the `EnumerationName`
struct:

```csharp
EqualityAssert.TestAllEqualityBehaviors(name1, name2, differentName);
```

### ResultAssert

Readable assertions for `Result<T, E>` and `UnitResult<E>` from CSharpFunctionalExtensions. It has **only**
`IsSuccess` — there is no `IsFailure`:

```csharp
result.IsSuccess();   // UnitResult<Error> or Result<T, Error>
```

For the failure path, assert on the result directly:

```csharp
Assert.True(result.IsFailure);
Assert.Equal("InvoiceMustBeDraft", result.Error.Code.Key);
```

## Test naming and structure

Method names: `snake_case` sentence describing the scenario and expected outcome:

```
Equals_successfully_returns_true_for_equal_parameters
CreateInvoice_with_null_customer_throws_validation_exception
GetInvoice_returns_not_found_when_invoice_does_not_exist
```

Three-section structure with `// Arrange`, `// Act`, `// Assert` comments:

```csharp
[Fact]
public async Task GetInvoice_returns_correct_invoice()
{
    // Arrange
    var client = _app.CreateClient();
    var invoiceId = await CreateTestInvoice(client);

    // Act
    var response = await client.GetAsync($"/invoices/{invoiceId}");

    // Assert
    response.EnsureSuccessStatusCode();
    var invoice = await response.Content.ReadFromJsonAsync<InvoiceDto>();
    Assert.NotNull(invoice);
    Assert.Equal(invoiceId, invoice.Id);
}
```

## Reference

[Demo test project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo.Tests):
- `DemoAppBuilder.cs` — subclassing `TestedAppBuilder`
- `Smoke/` — smoke tests in practice
- `Errors/` — error handling tests
- `Contexts/InvoiceDbContextTests.cs` — EF Core integration tests
