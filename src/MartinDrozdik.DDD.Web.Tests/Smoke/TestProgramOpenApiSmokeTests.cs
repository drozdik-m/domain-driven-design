using MartinDrozdik.DDD.Testing.Smoke;
using Xunit.Abstractions;

namespace MartinDrozdik.DDD.Web.Tests.Smoke;

public class TestProgramOpenApiSmokeTests(ITestOutputHelper testOutputHelper)
    : OpenApiSmokeTests<TestProgramFactoryBuilder, TestProgram>(new TestProgramFactoryBuilder(testOutputHelper))
{
    protected override IEnumerable<OpenApiEndpoint> GetOpenApiEndpoints()
    {
        return [
            new OpenApiEndpoint("/openapi/doc.json", OpenApiType.Json)
        ];
    }
}
