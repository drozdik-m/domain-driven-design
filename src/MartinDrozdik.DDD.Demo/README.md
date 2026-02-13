# MartinDrozdik.DDD.Demo - Reference Implementation

A working example of DDD patterns with ASP.NET Core using [MartinDrozdik.DDD](../MartinDrozdik.DDD) and [MartinDrozdik.DDD.Web](../MartinDrozdik.DDD.Web).

## Quick Start

```bash
# Clone and run
git clone https://github.com/drozdik-m/domain-driven-design.git
cd domain-driven-design/src/MartinDrozdik.DDD.Demo
dotnet run --launch-profile scalar
```

Or just use the Visual Studios' *Play button*.

Browse to:

- **Scalar API UI**: https://localhost:7124/scalar
- **OpenAPI Spec**: https://localhost:7124/openapi/v1.json

## Project Structure

```
MartinDrozdik.DDD.Demo/
├── Models/       # Domain layer (the good stuff)
├── Requests/     # CQRS stuff
├── Context/      # EF Core persistence
├── Controllers/  # HTTP endpoints
├── Options/      # App configuration
├── OpenApi/      # Auto-generated on build
└── Program.cs    # Everything wired together
```

## Domain stuff

Checkout the DDD examples in the `Models` folder. Here, you can find examples of:

- [InvoiceState.cs](./Models/Enumerations/InvoiceState.cs) Enumeration
- [InvoiceNumber.cs](./Models/ValueObjects/InvoiceNumber.cs) Value Object
- [Person.cs](./Models/Entities/Person.cs) Entity
- [Invoice.cs](./Models/Aggregates/Invoice.cs) Aggregate

## CQRS with DDD Mediator

Commands mutate state. Queries read state. They never mix.

- [GetInvoicesQuery.cs](./Requests/Invoices/GetInvoicesQuery.cs) + [GetInvoicesQueryHandler.cs](./Requests/Invoices/GetInvoicesQueryHandler.cs)
- [CreateInvoiceDraftCommand.cs](./Requests/Invoices/CreateInvoiceDraftCommand.cs) + [CreateInvoiceDraftCommandHandler.cs](./Requests/Invoices/CreateInvoiceDraftCommandHandler.cs)

## Configuration

Type-safe options that fail fast if misconfigured:

- [InvoiceOptions.cs](./Options/InvoiceOptions.cs) with FluentValidation-based validation

## Error Handling

All exceptions are automatically converted to RFC 7807 Problem Details responses by the middleware.

Check out the [ErrorController.cs](./Controllers/ErrorController.cs) for examples of how different exceptions are handled.

## EF Core Configuration

Shows proper mapping of DDD building blocks.

## OpenAPI Client Generation

The project auto-generates a type-safe C# client on every build using *Kiota*.

### OpenApi generation

Generate OpenAPI spec from your controllers using the `Microsoft.Extensions.ApiDescription.Server` and `Microsoft.AspNetCore.OpenApi` packages:

``` powershell
dotnet add package Microsoft.Extensions.ApiDescription.Server
dotnet add package Microsoft.AspNetCore.OpenApi
```

``` xml
  <!-- OpenAPI generation settings -->
  <PropertyGroup>
    <OpenApiGenerateDocumentsOptions>--file-name DemoApi</OpenApiGenerateDocumentsOptions>
    <OpenApiDocumentsDirectory>./OpenApi</OpenApiDocumentsDirectory>
  </PropertyGroup>
```

Then client code can be generated using generator tools like NSwag or Kiota.

### Kiota client generation

[Install Kiota tool](https://learn.microsoft.com/cs-cz/openapi/kiota/install)

``` powershell
dotnet tool install --global Microsoft.OpenApi.Kiota
```

Prepare the client project to receive generated code:

``` powershell
dotnet new classlib -n MartinDrozdik.DDD.Demo.Client
```

Install required NuGet packages to the client project:

``` powershell
dotnet add package Microsoft.Kiota.Bundle
```

Generate client code using Kiota automatically from the Api project:

``` powershell
<Target Name="OpenApi" AfterTargets="Build" Condition="$(Configuration)=='Debug'">
  <Exec Command="kiota generate -l CSharp --output ../MartinDrozdik.DDD.Demo.Client/Generated --namespace-name MartinDrozdik.DDD.Demo.Client.Generated --class-name DddClient --exclude-backward-compatible --openapi ./OpenApi/DemoApi.json" WorkingDirectory="$(ProjectDir)" />
</Target>
```

Every time you build the project, the client regenerates to match your API. No manual sync needed.

## Further Reading

- [Core DDD Library](../MartinDrozdik.DDD) - Building blocks and patterns
- [Web DDD Library](../MartinDrozdik.DDD.Web) - ASP.NET Core integration
