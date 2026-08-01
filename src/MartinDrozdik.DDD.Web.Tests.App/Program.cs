using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Web;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.RecurringTasks;
using MartinDrozdik.DDD.Web.Tests.App;
using Microsoft.EntityFrameworkCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);
builder.AddAppServices();

// Add DbContext with SQLite
builder.AddAppDbContext<TestDbContext>((options, dbBuilder) =>
{
    dbBuilder.UseSqlite(options.ConnectionString);
});

// A recurring task scheduled far enough away that it only runs when a test triggers it
builder.Services.AddSingleton<TestRecurringTaskRuns>();
builder.AddRecurringTask<TestRecurringTask>(options =>
{
    options.InitialDelay = TimeSpan.FromHours(1);
    options.Period = TimeSpan.FromHours(1);
});

// --- APP ---
var app = builder.Build();

await app.EnsureCreatedDatabaseAsync<TestDbContext>();

app.UseAppMiddlewares();

app.MapOpenApi("/openapi/doc.json");
app.MapOpenApi("/openapi/doc.yaml");

app.MapGet("/", () => "Hello World!");

// Endpoints throwing business exceptions, used to verify how the middleware pipeline logs them
app.MapGet("/throw/not-found", string () => throw new BusinessNotFoundException("Nothing here"));
app.MapGet("/throw/unhandled", string () => throw new InvalidOperationException("Boom"));

await app.RunAsync();
