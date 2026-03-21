using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Demo.Tests.Smoke;

public class DemoAppOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<Program>(new DemoAppBuilder(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        return [
            new OpenApiEndpoint("/openapi/v1.json", OpenApiType.Json)
        ];
    }
}
