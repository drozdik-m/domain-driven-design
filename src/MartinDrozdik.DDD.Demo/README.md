# Demo project for nice DDD and webdev practices

## OpenApi generation

Added OpenApi generation to the project that is created on build.

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

[Install Kiota tool](https://learn.microsoft.com/cs-cz/openapi/kiota/install?tabs=bash)

``` powershell
dotnet tool install --global Microsoft.OpenApi.Kiota
```

Prepare the client project to receive generated code:

``` powershell
dotnet new classlib -n MartinDrozdik.DDD.Demo.Client
```

Install required NuGet packages to the client project:

- `Microsoft.Kiota.Bundle`

Generate client code using Kiota automatically from the Api project:

``` powershell
<Target Name="OpenApi" AfterTargets="Build" Condition="$(Configuration)=='Debug'">
  <Exec Command="kiota generate -l CSharp --output ../MartinDrozdik.DDD.Demo.Client/Generated --namespace-name MartinDrozdik.DDD.Demo.Client.Generated --class-name DddClient --exclude-backward-compatible --openapi ./OpenApi/DemoApi.json" WorkingDirectory="$(ProjectDir)" />
</Target>
```
