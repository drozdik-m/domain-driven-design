using MartinDrozdik.DDD.Integrations;
using MartinDrozdik.DDD.Testing.Smoke;

namespace MartinDrozdik.DDD.Demo.Tests.Smoke;
/*
public class DemoAppEndpointSmokeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [MemberData(nameof(Endpoints))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Delegated to tester.")]
    public async Task Test_smoke_endpoint(EndpointTest testCase)
    {
        var builder = new DemoAppBuilder(testOutputHelper);
        var tester = new EndpointSmokeTester<Program>(builder);
        await tester.Test(testCase, TestContext.Current.CancellationToken);
    }

    public static IEnumerable<TheoryDataRow<EndpointTest>> Endpoints()
    {
        var root = new UrlBuilder("v1", "invoice")
        yield return new EndpointTest(HttpMethod.Get, root.Build())
    }
}
*/
