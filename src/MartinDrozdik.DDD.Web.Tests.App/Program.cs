using MartinDrozdik.DDD.Web;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices();

// Add DbContext with SQLite
var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_test_db.db");
var connectionString = $"Data Source={dbPath}";
builder.Configuration[$"{DatabaseOptions.Section}:{nameof(DatabaseOptions.ConnectionString)}"] = connectionString;
builder.AddAppDbContext<TestDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

// --- APP ---
var app = builder.Build();

await app.EnsureCreatedDatabaseAsync<TestDbContext>();

app.UseAppMiddlewares();

app.MapOpenApi("/openapi/doc.json");

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
