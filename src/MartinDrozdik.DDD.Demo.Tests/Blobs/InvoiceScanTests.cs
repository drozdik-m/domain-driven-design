using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using MartinDrozdik.DDD.Blobs;
using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Blobs.Stores;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Models.Aggregates;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Testing.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MartinDrozdik.DDD.Demo.Tests.Blobs;

public class InvoiceScanTests
{
    private readonly TestedApp<Program> _factory;

    public InvoiceScanTests(ITestOutputHelper testOutputHelper)
    {
        _factory = new DemoAppBuilder(testOutputHelper).Build();
    }

    [Fact]
    public async Task A_scan_can_be_uploaded_and_downloaded_again()
    {
        // Arrange
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();

        // Act
        var scanId = await UploadAsync(client, invoiceId, "Faktura 2026 (final).pdf", "the scan");
        var download = await client.GetAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("the scan", await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Offered under the name it arrived with, not the sanitized one it is stored under
        Assert.Equal("Faktura 2026 (final).pdf", download.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.NotNull(download.Headers.ETag);
        Assert.NotEqual(Guid.Empty, scanId);
    }

    [Fact]
    public async Task An_uploaded_scan_is_stored_under_a_sanitized_name_in_a_folder_of_its_own()
    {
        // Arrange
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();

        // Act
        var scanId = await UploadAsync(client, invoiceId, "../../etc/passwd.pdf", "the scan");

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var blob = await context.Set<Blob>().AsNoTracking().SingleAsync(b => b.Id == new BlobId(scanId), TestContext.Current.CancellationToken);

        Assert.Equal("passwd.pdf", blob.Name.Value);
        Assert.Equal("../../etc/passwd.pdf", blob.OriginalFileName);
        Assert.Equal(scanId.ToString(), blob.Id.ToString());
    }

    [Fact]
    public async Task Deleting_a_scan_removes_the_file_only_once_the_outbox_delivers()
    {
        // Arrange
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();
        var scanId = await UploadAsync(client, invoiceId, "report.pdf", "the scan");

        var store = _factory.Services.GetRequiredService<IBlobStore>();
        var key = await GetKeyAsync(scanId);

        // Act
        var deleted = await client.DeleteAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // The row and the reference are gone, but the file is still there - the message has not been delivered yet
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var invoice = await context.Invoices.AsNoTracking().SingleAsync(i => i.Id == new InvoiceId(invoiceId), TestContext.Current.CancellationToken);
            Assert.Null(invoice.ScanId);
            Assert.False(await context.Set<Blob>().AsNoTracking().AnyAsync(b => b.Id == new BlobId(scanId), TestContext.Current.CancellationToken));
        }

        Assert.True(await store.ExistsAsync(key, TestContext.Current.CancellationToken));

        await _factory.DrainOutboxAsync(TestContext.Current.CancellationToken);

        Assert.False(await store.ExistsAsync(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deleting_a_scan_whose_file_went_missing_still_succeeds()
    {
        // Arrange
        // Somebody cleared the folder by hand. The invoice cannot know, and should not have to.
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();
        var scanId = await UploadAsync(client, invoiceId, "report.pdf", "the scan");

        var store = _factory.Services.GetRequiredService<IBlobStore>();
        var key = await GetKeyAsync(scanId);
        await store.DeleteAsync(key, TestContext.Current.CancellationToken);

        // Act
        var deleted = await client.DeleteAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);
        await _factory.DrainOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // The deletion was delivered rather than dead-lettered: a file that is already gone is the outcome
        // the message asked for, not a failure to retry
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var deadLettered = await context.Set<MartinDrozdik.DDD.Web.Outbox.Models.OutboxMessage>()
            .AsNoTracking()
            .CountAsync(m => m.FailedAt != null, TestContext.Current.CancellationToken);
        Assert.Equal(0, deadLettered);
    }

    [Fact]
    public async Task Deleting_a_scan_that_is_not_there_succeeds()
    {
        // Arrange
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();

        // Act
        var deleted = await client.DeleteAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Uploading_a_second_scan_replaces_the_first_and_enqueues_its_removal()
    {
        // Arrange
        // A blob is immutable, so replacing one means storing the new and queueing the old for deletion
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();
        var firstId = await UploadAsync(client, invoiceId, "first.pdf", "first");
        var firstKey = await GetKeyAsync(firstId);

        var store = _factory.Services.GetRequiredService<IBlobStore>();

        // Act
        var secondId = await UploadAsync(client, invoiceId, "second.pdf", "second");
        await _factory.DrainOutboxAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(firstId, secondId);
        Assert.False(await store.ExistsAsync(firstKey, TestContext.Current.CancellationToken));

        var download = await client.GetAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal("second", await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Downloading_a_scan_whose_file_went_missing_reports_it_as_not_found_and_keeps_the_row()
    {
        // Arrange
        var invoiceId = await CreateInvoiceAsync();
        using var client = _factory.CreateClient();
        var scanId = await UploadAsync(client, invoiceId, "report.pdf", "the scan");

        var store = _factory.Services.GetRequiredService<IBlobStore>();
        await store.DeleteAsync(await GetKeyAsync(scanId), TestContext.Current.CancellationToken);

        // Act
        var download = await client.GetAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        Assert.True(await context.Set<Blob>().AsNoTracking().AnyAsync(b => b.Id == new BlobId(scanId), TestContext.Current.CancellationToken));
    }

    private static async Task<Guid> UploadAsync(HttpClient client, Guid invoiceId, string fileName, string content)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        var response = await client.PostAsync(new Uri($"/v1/invoice/{invoiceId}/scan", UriKind.Relative), form, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>(TestContext.Current.CancellationToken);
    }

    private async Task<Guid> CreateInvoiceAsync()
    {
        using var client = _factory.CreateClient();

        var request = new
        {
            recipient = new { name = Guid.NewGuid().ToString(), dateOfBirth = DateTimeOffset.UnixEpoch },
        };

        var response = await client.PostAsJsonAsync(new Uri("/v1/invoice", UriKind.Relative), request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<InvoiceIdResponse>(TestContext.Current.CancellationToken);

        return created!.Key;
    }

    private async Task<BlobKey> GetKeyAsync(Guid scanId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var blob = await context.Set<Blob>().AsNoTracking().SingleAsync(b => b.Id == new BlobId(scanId), TestContext.Current.CancellationToken);

        return blob.Key;
    }

    /// <summary>
    /// The shape the invoice endpoint answers a creation with.
    /// </summary>
    /// <param name="Key">The identity of the new invoice.</param>
    private sealed record InvoiceIdResponse(Guid Key);
}
