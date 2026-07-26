/*using MartinDrozdik.DDD.Demo.Client.Generated.Models;
using MartinDrozdik.DDD.Testing.Builders;

namespace MartinDrozdik.DDD.Demo.Tests.Builders;

/// <summary>
/// Builder for the mutable Kiota client model <see cref="CreateInvoiceDraftCommandPerson"/> that tests POST over HTTP.
/// The generator supplies <c>WithName</c> / <c>WithDateOfBirth</c> and skips the Kiota <c>AdditionalData</c> bag.
/// </summary>
[TestDataBuilder(typeof(CreateInvoiceDraftCommandPerson))]
public partial class CreateInvoiceDraftCommandPersonBuilder
{
    /// <inheritdoc/>
    protected override CreateInvoiceDraftCommandPerson CreateDefault() => new()
    {
        Name = Guid.NewGuid().ToString(),
        DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
    };
}
*/
