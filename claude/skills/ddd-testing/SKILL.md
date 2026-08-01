---
description: Use when writing tests with MartinDrozdik.DDD.Testing — TestedApp, TestedAppBuilder, smoke tests (WebApplicationSmokeTests, OpenApiSmokeTests, ErrorHandlingTests), EF Core integration tests with SqlDbContextIntegrationTests, EqualityAssert for value objects, or ResultAssert for Result<T>.
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
        .WithOption<SomeOptions>(o => o.Flag, true)
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
    .WithConfig(builder => builder.UseSetting("Key", "Value"))
    .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
    .WithServices(services => services.AddSingleton<IMyService, MockMyService>())
    .WithEndpoints(endpoints => endpoints.MapGet("/test-route", () => Results.Ok()))
    .WithDisposable(() => Console.WriteLine("cleaned up"))
    .WithEnvironment("Development")                   // override environment
    .WithUserAndRoles("testuser", ["Admin", "User"])  // fake authenticated user
    .WithClaimsPrincipal(myClaimsPrincipal)           // full claims control
    .WithRecurringTasks()                             // keep background loops (removed by default)
    .WithoutHostedService<MyWorker>()                 // drop one of your own hosted services
    .Build();
```

## Recurring Tasks

`Build()` **removes every recurring task loop by default**, so background work never fires mid-test. The tasks stay registered — only the loops are gone.

```csharp
// Run one iteration on demand, in a fresh scope, exactly as the loop would.
// Rethrows what the task throws; ignores Enabled and Timeout.
await app.RunRecurringTaskAsync<CleanupTask>(TestContext.Current.CancellationToken);

app.GetRecurringTaskSchedule<CleanupTask>();   // the schedule in effect
app.TriggerRecurringTask<CleanupTask>();       // no-op unless .WithRecurringTasks() was used
```

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

    public void Dispose() => _app.Dispose();
}
```

## Assertions

### EqualityAssert — for Value Objects

Tests `IEquatable<T>`, `IEqualityComparer<T>`, and `==`/`!=` operators in one call. Covers symmetry, null comparisons, and hash code consistency.

Parameters: `a` and `b` must be equal (same value, different instances); `c` must be unequal.

```csharp
[Fact]
public void InvoiceNumber_equality_is_correct()
{
    var a = InvoiceNumber.Create(2024, 1);
    var b = InvoiceNumber.Create(2024, 1); // equal to a
    var c = InvoiceNumber.Create(2024, 2); // not equal to a

    EqualityAssert.TestAllEqualityBehaviors(a, b, c);
}
```

Test individual aspects:

```csharp
EqualityAssert.TestEquatable(a, b, c);        // IEquatable<T>
EqualityAssert.TestEqualityComparer(a, b, c); // IEqualityComparer<T>
EqualityAssert.TestEqualityOperators(a, b, c);// == and !=
```

### ResultAssert — for Result\<T\>

Readable assertions for `Result<T>` and `UnitResult<E>` from CSharpFunctionalExtensions:

```csharp
result.IsSuccess();
result.IsFailure();
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
