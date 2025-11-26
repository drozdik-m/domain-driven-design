using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Requests.Invoice;
using MartinDrozdik.DDD.Models.Mediator;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();

// Add DbContext with SQLite
builder.Services.AddDbContext<InvoiceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlite(connectionString);

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMediator(config =>
{
    config.WithQuery<GetInvoicesQuery, GetInvoicesQuery.Response, GetInvoicesQueryHandler>();
});

// --- APP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
