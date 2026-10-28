using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Outbox.Retention;

/// <summary>
/// How the retention of the <typeparamref name="TDbContext"/> outbox deletes delivered messages.
/// </summary>
/// <typeparam name="TDbContext">The context that owns the outbox.</typeparam>
#pragma warning disable S2326 // Unused type parameters - used to mark the outbox context
public sealed class OutboxRetentionOptions<TDbContext>
#pragma warning restore S2326
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets or sets how long delivered messages are kept before <see cref="IOutboxRetention{TDbContext}.ApplyAsync"/> deletes them.
    /// Has no default, it must be set.
    /// </summary>
    /// <remarks>
    /// Only delivered messages are deleted. Dead-lettered ones stay until a person has dealt with them.
    /// Do not add the retention at all if you want to keep delivered messages forever.
    /// </remarks>
    public TimeSpan Retention { get; set; }

    /// <summary>
    /// Gets or sets how many delivered messages one delete statement removes.
    /// </summary>
    /// <remarks>
    /// The retention deletes in chunks, so it never holds the table for long.
    /// Stay below 5000 rows - SQL Server escalates the locks of one statement to the whole table.
    /// </remarks>
    public int BatchSize { get; set; } = 1000;
}
