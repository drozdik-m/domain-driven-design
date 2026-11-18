using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.Outbox.Shipping;

/// <summary>
/// A context whose class name another module uses too, to check that its outbox keeps a name of its own.
/// </summary>
/// <param name="options">The options of the context.</param>
public class ModuleDbContext(DbContextOptions<ModuleDbContext> options) : DbContext(options)
{
}
