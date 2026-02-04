using MartinDrozdik.DDD.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MartinDrozdik.DDD.Web.Databases;

/// <summary>
/// Extensions for <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds SQL database context to the <see cref="IHostApplicationBuilder"/>.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    public static IHostApplicationBuilder AddAppDbContext<T>(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder> contextBuilder)
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
    /// Adds SQL database context to the <see cref="IHostApplicationBuilder"/>.
    /// Sets up and uses <see cref="DatabaseOptions"/> for configuration.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/> using <see cref="DatabaseOptions"/>.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    public static IHostApplicationBuilder AddAppDbContext<T>(this IHostApplicationBuilder builder, Action<DatabaseOptions, DbContextOptionsBuilder> contextBuilder)
        where T : DbContext
    {
        builder.Services.AddValidatedAppOptions<DatabaseOptions>();
        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseOptions>>();

        void setup(DbContextOptionsBuilder dbBuilder) => contextBuilder(options.Value, dbBuilder);

        return builder.AddAppDbContext<T>(setup);
    }
}
