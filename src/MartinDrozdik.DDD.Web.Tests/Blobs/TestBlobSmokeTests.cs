using MartinDrozdik.DDD.Testing.Blobs;
using MartinDrozdik.DDD.Web.Tests.App;

namespace MartinDrozdik.DDD.Web.Tests.Blobs;

public class TestBlobSmokeTests(ITestOutputHelper testOutputHelper)
    : BlobSmokeTests<Program, TestDbContext>(new TestedWebAppBuilder(testOutputHelper), TestBlobContainers.Invoices, TestBlobContainers.Avatars)
{
}
