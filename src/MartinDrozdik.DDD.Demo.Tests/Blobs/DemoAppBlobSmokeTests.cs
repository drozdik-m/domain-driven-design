using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models;
using MartinDrozdik.DDD.Testing.Blobs;

namespace MartinDrozdik.DDD.Demo.Tests.Blobs;

public class DemoAppBlobSmokeTests(ITestOutputHelper testOutputHelper)
    : BlobSmokeTests<Program, InvoiceDbContext>(new DemoAppBuilder(testOutputHelper), BlobContainers.InvoiceScans)
{
}
