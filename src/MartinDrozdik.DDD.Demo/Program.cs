using System.Net.Mime;
using MartinDrozdik.DDD.Demo.Context;
using MartinDrozdik.DDD.Demo.Middlewares.Exceptions;
using MartinDrozdik.DDD.Demo.Requests.Invoices;
using MartinDrozdik.DDD.Models.Mediator;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// --- BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();

// Add error handling
builder.Services.AddProblemDetails()
    .AddExceptionHandler<BusinessRuleValidationExceptionHandler>()
    .AddExceptionHandler<ValidationExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

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

// Ensure DB is created
try
{
    await using var context = app.Services.CreateAsyncScope();
    using var dbContext = context.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An error occurred creating the DB.");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Set up handler for json error responses
// TODO make this more robust and reusable, handle validation exceptions correctly
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = app.Environment.IsDevelopment()
                ? exceptionHandlerPathFeature?.Error.ToString()
                : "An unexpected error occurred while processing your request.",
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
