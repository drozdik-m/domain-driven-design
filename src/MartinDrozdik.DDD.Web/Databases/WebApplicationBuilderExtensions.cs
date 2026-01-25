using MartinDrozdik.DDD.Web.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds SQL database context to the <see cref="WebApplicationBuilder"/>.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
    /// <returns>Updated <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder AddAppDbContext<T>(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> contextBuilder)
        where T : DbContext
    {
        builder.Services.AddDbContext<T>(options =>
        {
            contextBuilder(options);

            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        return builder;
    }

    /// <summary>
    /// Adds SQL database context to the <see cref="WebApplicationBuilder"/>.
    /// Sets up and uses <see cref="DatabaseOptions"/> for configuration.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/> using <see cref="DatabaseOptions"/>.</param>
    /// <returns>Updated <see cref="WebApplicationBuilder"/>.</returns>
    public static WebApplicationBuilder AddAppDbContext<T>(this WebApplicationBuilder builder, Action<DatabaseOptions, DbContextOptionsBuilder> contextBuilder)
        where T : DbContext
    {
        builder.Services.AddValidatedAppOptions<DatabaseOptions>();
        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseOptions>>();

        void setup(DbContextOptionsBuilder dbBuilder) => contextBuilder(options.Value, dbBuilder);

        return builder.AddAppDbContext<T>(setup);
    }
}
