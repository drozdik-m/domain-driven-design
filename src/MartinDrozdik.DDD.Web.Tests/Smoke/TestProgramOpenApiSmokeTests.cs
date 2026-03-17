using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<TestWebApplicationFactoryBuilder<TestProgram>, TestProgram>(new TestWebApplicationFactoryBuilder<TestProgram>(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        return [
            new OpenApiEndpoint("/openapi/doc.json", OpenApiType.Json)
        ];
    }
}
