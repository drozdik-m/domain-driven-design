using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Demo.Tests.Smoke;

public class DemoAppOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<DemoAppFactoryBuilder, Program>(new DemoAppFactoryBuilder(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        return [
            new OpenApiEndpoint("/openapi/v1.json", OpenApiType.Json)
        ];
    }
}
