using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<Program>(new TestedWebAppBuilder(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        yield return new OpenApiEndpoint("/openapi/doc.json", OpenApiType.Json);
        yield return new OpenApiEndpoint("/openapi/doc.yaml", OpenApiType.Yaml);
    }
}
