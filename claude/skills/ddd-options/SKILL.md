---
description: Use when defining or registering configuration with MartinDrozdik.DDD.Options — IAppOptions, IValidatedAppOptions, AddAppOptions, AddValidatedAppOptions, or reading options during startup through the configuration manager. Works without ASP.NET Core, so it also applies to business and infrastructure layers.
---

You are an expert in the **MartinDrozdik.DDD.Options** library. Generate correct configuration setup using its specific APIs.

## Request

$ARGUMENTS

---

**No ASP.NET Core dependency.** This package works in any layer — domain, infrastructure, worker services, console apps — not just web hosts.

Install: `dotnet add package MartinDrozdik.DDD.Options`

---

## Validated Options

Use `IValidatedAppOptions<T>` when options must be validated on startup (fail-fast). Use `IAppOptions` for options with no validation requirement.

```csharp
public class InvoiceOptions : IValidatedAppOptions<InvoiceOptions>
{
    public static string Section => "App:Invoice";
    public static AbstractValidator<InvoiceOptions> Validator { get; } = new OptionsValidator();

    public required int StartingId { get; init; }
    public required string DefaultName { get; init; }

    private class OptionsValidator : AbstractValidator<InvoiceOptions>
    {
        public OptionsValidator()
        {
            RuleFor(e => e.StartingId).GreaterThanOrEqualTo(0);
            RuleFor(e => e.DefaultName).NotNull().NotEmpty();
        }
    }
}

builder.Services.AddValidatedAppOptions<InvoiceOptions>();
```

```csharp
public class SimpleOptions : IAppOptions
{
    public static string Section => "App:Simple";
    public required string Value { get; init; }
}

builder.Services.AddAppOptions<SimpleOptions>();
```

Both bind strictly (`ErrorOnUnknownConfiguration = true`) and validate on start, so a typo in a config key is a
startup failure rather than a silent default.

To read options *during* startup, before the container exists, go through the configuration manager:

```csharp
var db = builder.Configuration.GetRequiredValidatedOptions<DatabaseOptions>();
// also: GetOptions<T>(), GetRequiredOptions<T>(), GetValidatedOptions<T>()
```

`ValidateOnStart` is executed by the generic host. Code that builds a bare `ServiceProvider` without a host must
resolve `IStartupValidator` and call `Validate()` itself.

## Reference

[Demo project](https://github.com/drozdik-m/domain-driven-design/tree/main/src/MartinDrozdik.DDD.Demo) — Options/ for options examples, Program.cs for registration.

For `IWebHostBuilder.SetOption<TOptions>()` and the rest of the ASP.NET Core wiring, use the `ddd-web` skill.
