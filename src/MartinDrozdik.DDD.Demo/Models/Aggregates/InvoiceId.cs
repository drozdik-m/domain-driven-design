using MartinDrozdik.DDD.Identities.Primitive;

namespace MartinDrozdik.DDD.Demo.Models.Aggregates;

/// <summary>
/// Identity of <see cref="InvoiceId"/> aggregate.
/// </summary>
public class InvoiceId(Guid key) : GuidIdentity<InvoiceId>(key);
