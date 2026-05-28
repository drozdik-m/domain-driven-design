# DDD Testing Library

[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD.Testing?style=flat-square&logo=nuget&label=MartinDrozdik.DDD.Testing)](https://www.nuget.org/packages/MartinDrozdik.DDD.Testing)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD.Testing?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/MartinDrozdik.DDD.Testing)
[![Build & Test](https://img.shields.io/github/actions/workflow/status/drozdik-m/domain-driven-design/main.yml?branch=main&style=flat-square&logo=github&label=actions)](https://github.com/drozdik-m/domain-driven-design/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/drozdik-m/domain-driven-design?style=flat-square)](LICENSE)

Common test tooling for the DDD libraries. Provides test fixtures, utilities, and helpers to make testing your DDD applications easier and more consistent. Check the [demo tests](../MartinDrozdik.DDD.Demo.Tests) for examples of how to use it in real tests.

## Installation

```bash
dotnet add package xunit.v3
dotnet add package MartinDrozdik.DDD.Testing
```

## Philosophy

- **Write tests, not test infrastructure** - Stop copy-pasting the same `WebApplicationFactory` boilerplate across every project.
- **Integration tests, not unit tests** - Test the whole stack with real dependencies – reveals much more bugs. Unit tests still valuable tho.
- **Composition over inheritance** – Avoid the giant base test class pitfall.
- **Consistent with the rest of the DDD stack** - Same pragmatic approach, same "use what makes sense" attitude.
- **xUnit** - It has more stars than *NUnit* lmao.

*You still have to write your own tests. But at least you don't have to write the boring parts.*

## Quick Start

Spin up your app in memory, make requests, assert things. Zero extra ceremony:

```csharp
// 1. Subclass the builder for your Program.cs (once, in your test project)
public class MyAppBuilder : TestedAppBuilder<Program>
{
    public MyAppBuilder(ITestOutputHelper output) : base(output)
    {
        // Add more configuration here
        // Or leave it empty. Nobody's judging.
    }
}

// 2. Use it in your tests
public class MyTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestedApp<Program> _app = new MyAppBuilder(output).Build();

    // Or set specific extra config for this test class
    private readonly TestedApp<Program> _app = new MyAppBuilder(output)
        .WithOption<TestOptions>(e => e.SomeString, value)
        .WithServices(services => services.AddAppOptions<TestOptions>())
        .Build();

    [Fact]
    public async Task Something_works()
    {
        var client = _app.CreateClient();
        var response = await client.GetAsync("/something");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Something_other_works()
    {
        // Or use the builder directly here
        var app = new MyAppBuilder(output).Build();
        var client = app.CreateClient();
        var response = await client.GetAsync("/something");
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _app.Dispose();
}
```

## TestedApp & TestedAppBuilder

The `TestedApp<TProgram>` wraps `WebApplicationFactory<TProgram>` (yes, the standard one) and adds the things you always end up adding yourself anyway:

- **xUnit log output** – Your logs actually show up in the test runner. *revoLuTiOnAry*.
    - *You would be surprised how many people don't do this – and then wonder why they have no logs when their tests fail.*
- **Test endpoint injection** – Register extra endpoints at test time without touching your production app.
- **Config and options overrides** – Change settings for tests without affecting production defaults.
- **Disposable tracking** – Attach anything that needs cleanup and it gets disposed with the factory.
- Mode minor goodies

Build it with the fluent `TestedAppBuilder<TProgram>`:

```csharp
var app = new MyAppBuilder(output)
    .With(builder => builder.UseSetting("SomeSetting", "SomeValue"))
    .WithOption<DatabaseOptions>(o => o.ConnectionString, "Data Source=:memory:")
    .WithServices(services => services.AddSingleton<IMyService, MockMyService>())
    .WithEndpoints(endpoints => endpoints.MapGet("/test-only-route", () => "hello"))
    .WithDisposable(() => Console.WriteLine("cleaned up, congrats"))
    .WithEnvironment(AppEnvironments.Testing)
    .WithUserAndRoles("testuser", ["Admin", "User"])
    .WithClaimsPrincipal(...)
    .Build();
```

Each `With*` call stacks – multiple configs, multiple endpoints, multiple disposables, all applied in order. Readable. Composable. Not a pyramid of constructors.

Call `Dispose()` when you're done.

## Testing environment

By default, the **tests run in the "Testing" environment**. You can change that via the `.WithEnvironment("Development")` builder method.

This is to **strictly separate your test configuration from your development configuration**. You can have different appsettings files, different DI registrations, etc., for "Testing" vs "Development".

Use the `AppEnvironments.Testing` constant or the *classic* `app.Environment.IsTesting()` to check if you're running in the Testing environment.

## Smoke Tests

Free tests that verify the basics. Because "it starts and doesn't explode" is a valid and surprisingly often-failing test.

Verify the app starts and is healthy:

```csharp
public class MyAppSmokeTests(ITestOutputHelper output)
    : WebApplicationSmokeTests<Program>(new MyAppBuilder(output))
{
}
```

Verifies that your OpenAPI document is valid JSON or YAML:

```csharp
public class MyOpenApiSmokeTests(ITestOutputHelper output)
    : OpenApiSmokeTests<Program>(new MyAppBuilder(output))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints() =>
    [
        new OpenApiEndpoint("/openapi/v1.json", OpenApiType.Json),
        new OpenApiEndpoint("/openapi/v1.yaml", OpenApiType.Yaml),
    ];
}
```

Tests your error handling is wired up correctly:

```csharp
public class MyAppErrorHandlingTests(ITestOutputHelper output)
    : ErrorHandlingTests<Program>(new MyAppBuilder(output))
{
}
```

## EF Core Context Tests

Tests that your EF Core mappings actually work against a real database. Because "the migration ran" does not mean "the query works":

```csharp
public class MyDbContextTests(ITestOutputHelper testOutputHelper) : SqlDbContextIntegrationTests<MyDbContext>, IDisposable
{
    private readonly TestedApp<Program> _factory =
        new DemoAppBuilder(testOutputHelper).Build();

    public void Dispose()
    {
        _factory.Dispose();
    }

    protected override MyDbContext GetContext()
    {
        return _factory.GetScopedService<MyDbContext>();
    }
}
```

## Assertions

### EqualityAssert

Testing equality implementations is tedious and it's easy to miss edge cases. `EqualityAssert` covers `IEquatable<T>`, `IEqualityComparer<T>`, and equality operators (`==`, `!=`) in one shot:

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

Or test just the parts you care about:

```csharp
EqualityAssert.TestEquatable(a, b, c);           // IEquatable<T>
EqualityAssert.TestEqualityComparer(a, b, c);    // IEqualityComparer<T>  
EqualityAssert.TestEqualityOperators(a, b, c);   // == and != operators
```

Covers symmetry, null/default comparisons, and hash code consistency. Basically everything you'd forget to test manually.

### ResultAssert

If you're using `Result<T, E>` or `UnitResult<E>` from [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions), these extension methods give you readable assertions:

```csharp
[Fact]
public void CreateInvoice_returns_success()
{
    var result = Invoice.Create(InvoiceNumber.Create(2024, 1));
    result.IsSuccess();
}
```

## Demo Tests

The [demo test project](../MartinDrozdik.DDD.Demo.Tests) shows all of this wired together with a real app. Check:

- [DemoAppBuilder.cs](../MartinDrozdik.DDD.Demo.Tests/DemoAppBuilder.cs) – how to subclass `TestedAppBuilder`
- [Smoke/*](../MartinDrozdik.DDD.Demo.Tests/Smoke) – smoke tests in practice
- [Errors/TestProgramErrorHandlingTests.cs](../MartinDrozdik.DDD.Demo.Tests/Errors/TestProgramErrorHandlingTests.cs) – error handling tests in practice
- [Contexts/InvoiceDbContextTests.cs](../MartinDrozdik.DDD.Demo.Tests/Contexts/InvoiceDbContextTests.cs) – EF Core integration tests in practice
