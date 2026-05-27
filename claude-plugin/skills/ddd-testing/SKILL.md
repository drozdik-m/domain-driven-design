---
description: Use when writing tests with MartinDrozdik.DDD.Testing — TestedApp, TestedAppBuilder, smoke tests (WebApplicationSmokeTests, OpenApiSmokeTests, ErrorHandlingTests), EF Core integration tests with SqlDbContextIntegrationTests, EqualityAssert for value objects, or ResultAssert for Result<T>.
---

You are an expert in the **MartinDrozdik.DDD.Testing** library. Help the user write integration tests using this specific library.

## Library philosophy

- **Write tests, not test infrastructure** — stop copy-pasting `WebApplicationFactory` boilerplate
- **Integration tests over unit tests** — test the whole stack with real dependencies; reveals far more bugs
- **Composition over inheritance** — avoid the giant base test class pitfall
- Tests run in the **"Testing" environment** by default, isolated from "Development" config

Install:
```bash
dotnet add package xunit.v3
dotnet add package MartinDrozdik.DDD.Testing
```

## User request

$ARGUMENTS

---

## TestedAppBuilder — one-time setup per test project

Subclass `TestedAppBuilder<TProgram>` once in your test project. Reference your ASP.NET Core `Program` class:

```csharp
public class MyAppBuilder(ITestOutputHelper output) : TestedAppBuilder<Program>(output)
{
    // Add shared overrides that apply to every test:
    // .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
    // .WithEnvironment(AppEnvironments.Testing)  ← default, usually leave out
}
```

## TestedApp — using the app in tests

Build and use in test classes:

```csharp
public class MyTests(ITestOutputHelper output) : IDisposable
{
    // Shared instance (faster, but state leaks between tests)
    private readonly TestedApp<Program> _app = new MyAppBuilder(output).Build();

    // Or per-test overrides inline:
    private readonly TestedApp<Program> _app = new MyAppBuilder(output)
        .WithOption<SomeOptions>(o => o.Flag, true)
        .WithServices(services => services.AddSingleton<IMyService, FakeMyService>())
        .WithEndpoints(endpoints => endpoints.MapGet("/test-only", () => "hello"))
        .Build();

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

## Full builder API

All `With*` calls stack in order and are additive:

```csharp
new MyAppBuilder(output)
    .WithConfig(builder => builder.UseSetting("Key", "Value"))
    .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
    .WithServices(services => services.AddSingleton<IMyService, MockMyService>())
    .WithEndpoints(endpoints => endpoints.MapGet("/test-route", () => Results.Ok()))
    .WithDisposable(() => Console.WriteLine("cleaned up"))
    .WithEnvironment("Development")                       // override environment
    .WithUserAndRoles("testuser", ["Admin", "User"])      // fake authenticated user
    .WithClaimsPrincipal(myClaimsPrincipal)               // full claims control
    .Build();
```

## Smoke Tests

Free tests that verify the app starts and fundamentals work. Inherit from the base classes — no test body needed:

```csharp
// Verifies the app starts and health endpoints respond
public class MyAppSmokeTests(ITestOutputHelper output)
    : WebApplicationSmokeTests<Program>(new MyAppBuilder(output))
{
}

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
    : ErrorHandlingTests<Program>(new MyAppBuilder(output))
{
}
```

## EF Core Integration Tests

Test that EF Core mappings, migrations, and queries actually work against a real database:

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

Verifies entity mappings, model compilation, migrations, and basic connectivity.

## Assertions

### EqualityAssert — for Value Objects

Tests `IEquatable<T>`, `IEqualityComparer<T>`, and `==`/`!=` operators in one shot. Covers symmetry, null comparisons, and hash code consistency:

```csharp
[Fact]
public void InvoiceNumber_equality_is_correct()
{
    var a = InvoiceNumber.Create(2024, 1);
    var b = InvoiceNumber.Create(2024, 1); // same value, different instance
    var c = InvoiceNumber.Create(2024, 2); // different value

    EqualityAssert.TestAllEqualityBehaviors(a, b, c);
}
```

Test individual aspects:

```csharp
EqualityAssert.TestEquatable(a, b, c);           // IEquatable<T>
EqualityAssert.TestEqualityComparer(a, b, c);    // IEqualityComparer<T>
EqualityAssert.TestEqualityOperators(a, b, c);   // == and !=
```

### ResultAssert — for Result\<T\>

Readable assertions for `Result<T>` and `UnitResult<E>` from CSharpFunctionalExtensions:

```csharp
[Fact]
public void CreateInvoice_returns_success()
{
    var result = Invoice.Create(InvoiceNumber.Create(2024, 1));
    result.IsSuccess();
}

[Fact]
public void CreateInvoice_with_invalid_year_returns_failure()
{
    var result = Invoice.Create(InvoiceNumber.Create(1900, 1));
    result.IsFailure();
}
```

## Test naming conventions (xUnit v3)

Method names describe the test as a sentence in `snake_case`:

```
Equals_successfully_returns_true_for_equal_parameters
CreateInvoice_with_null_customer_throws_validation_exception
GetInvoice_returns_not_found_when_invoice_does_not_exist
```

Structure with three commented sections:

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

See the [demo test project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo.Tests):
- `DemoAppBuilder.cs` — how to subclass `TestedAppBuilder`
- `Smoke/` — smoke tests in practice
- `Errors/` — error handling tests
- `Contexts/InvoiceDbContextTests.cs` — EF Core integration tests
