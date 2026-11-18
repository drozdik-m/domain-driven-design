using MartinDrozdik.DDD.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox;

/// <summary>
/// Unified name for the outbox of a context in health checks, logs and exception messages.
/// </summary>
internal static class OutboxName
{
    /// <summary>
    /// Gets the name of the outbox of <typeparamref name="TDbContext"/>: its readable name prefixed with its namespace.
    /// </summary>
    /// <remarks>
    /// The namespace keeps apart two contexts with the same class name, e.g. <c>Billing.ModuleDbContext</c> and <c>Shipping.ModuleDbContext</c>.
    /// Prevents collisions.
    /// </remarks>
    /// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
    /// <returns>The name, e.g. <c>Billing.ModuleDbContext</c>.</returns>
    public static string Of<TDbContext>()
        where TDbContext : DbContext
    {
        var type = typeof(TDbContext);
        var name = type.GetReadableName();
        return string.IsNullOrEmpty(type.Namespace)
            ? name
            : $"{type.Namespace}.{name}";
    }
}
