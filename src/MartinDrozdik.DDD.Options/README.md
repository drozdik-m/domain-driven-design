# DDD Options - Configuration That Fails Fast

[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD.Options?style=flat-square&logo=nuget&label=MartinDrozdik.DDD.Options)](https://www.nuget.org/packages/MartinDrozdik.DDD.Options)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD.Options?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/MartinDrozdik.DDD.Options)
[![Build & Test](https://img.shields.io/github/actions/workflow/status/drozdik-m/domain-driven-design/main.yml?branch=main&style=flat-square&logo=github&label=actions)](https://github.com/drozdik-m/domain-driven-design/actions)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/drozdik-m/domain-driven-design?style=flat-square)](https://github.com/drozdik-m/domain-driven-design/blob/main/LICENSE)

`IOptions<T>` with a section convention and FluentValidation, based on [MartinDrozdik.DDD](../MartinDrozdik.DDD). No ASP.NET Core dependency, so your business layer can define and register its own options. Check the [demo](../MartinDrozdik.DDD.Demo).

## Installation

```bash
dotnet add package MartinDrozdik.DDD.Options
```

Also check this repos' [DDD Claude Code plugin](../../claude/README.md) for better AI code generation.

## Options

Leverages `IOptions<T>` with FluentValidation for configuration validation and automatic binding.

Configured to **fail fast** if your configuration is invalid. No more *"wOrKs On My MaChInE"*.

```csharp
public class InvoiceOptions : IValidatedAppOptions<InvoiceOptions>
{
    public static string Section => "App:Invoice"; // Simple binding from config (appsettings etc.)

    // FluentValidation validator, because attributes are for suckers
    public static AbstractValidator<InvoiceOptions> Validator { get; } = new OptionsValidator();

    // Your actual options properties:
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

// Register it:
builder.Services.AddValidatedAppOptions<InvoiceOptions>();
```

For simpler options without validation:

```csharp
public class SimpleOptions : IAppOptions
{
    public static string Section => "App:Simple";
    public required string Value { get; init; }
}

builder.Services.AddAppOptions<SimpleOptions>();
```

Both bind strictly (`ErrorOnUnknownConfiguration = true`) and validate on start, so a typo in a config key is a startup failure rather than a silent default.

To read options *during* startup, before the container exists, go through the configuration manager:

```csharp
var db = builder.Configuration.GetRequiredValidatedOptions<DatabaseOptions>();
// also: GetOptions<T>(), GetRequiredOptions<T>(), GetValidatedOptions<T>()
```

## Testing

Goes well with [MartinDrozdik.DDD.Testing](../MartinDrozdik.DDD.Testing) smoke tests, since options are validated on startup.

```csharp
public class DemoAppSmokeTests(ITestOutputHelper testOutputHelper)
    : WebApplicationSmokeTests<Program>(new DemoAppBuilder(testOutputHelper));
```
