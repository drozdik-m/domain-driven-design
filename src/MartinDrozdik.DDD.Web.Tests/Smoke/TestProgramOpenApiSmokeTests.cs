using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<TestProgramFactoryBuilder, Program>(new TestProgramFactoryBuilder(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        return [
            new OpenApiEndpoint("/openapi/doc.json", OpenApiType.Json),
            new OpenApiEndpoint("/openapi/doc.yaml", OpenApiType.Yaml)
        ];
    }
}
