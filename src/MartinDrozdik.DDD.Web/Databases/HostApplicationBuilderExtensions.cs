using MartinDrozdik.DDD.Options;
using MartinDrozdik.DDD.Web.Environments;
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
        ArgumentNullException.ThrowIfNull(contextBuilder);

        return builder.AddAppDbContextCore<T>((_, options) => contextBuilder(options));
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

    /// <summary>
    /// Adds SQL database context to the <see cref="IHostApplicationBuilder"/>,
    /// configured with both  <see cref="DatabaseOptions"/> and the services of the scope the context is resolved from.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/> using <see cref="DatabaseOptions"/> and the <see cref="IServiceProvider"/>.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// builder.AddAppDbContext&lt;InvoiceDbContext&gt;((options, provider, dbBuilder) =&gt;
    /// {
    ///     dbBuilder.UseSqlite(options.ConnectionString);
    ///     dbBuilder.AddInterceptors(provider.GetRequiredService&lt;OutboxTaskTriggerInterceptor&gt;());
    /// });
    /// </code>
    /// </example>
    public static IHostApplicationBuilder AddAppDbContext<T>(this IHostApplicationBuilder builder, Action<DatabaseOptions, IServiceProvider, DbContextOptionsBuilder> contextBuilder)
        where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contextBuilder);

        builder.Services.AddValidatedAppOptions<DatabaseOptions>();
        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseOptions>>();

        void Setup(IServiceProvider provider, DbContextOptionsBuilder dbBuilder)
            => contextBuilder(options.Value, provider, dbBuilder);

        return builder.AddAppDbContextCore<T>(Setup);
    }

    /// <summary>
    /// Adds SQL database context to the <see cref="IHostApplicationBuilder"/>,
    /// configured with the services of the scope the context is resolved from.
    /// Enables sensitive data logging and detailed errors in development environment.
    /// </summary>
    /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to extend.</param>
    /// <param name="contextBuilder">Action to configure the <see cref="DbContextOptionsBuilder"/> using the <see cref="IServiceProvider"/>.</param>
    /// <returns>Updated <see cref="IHostApplicationBuilder"/>.</returns>
    private static IHostApplicationBuilder AddAppDbContextCore<T>(this IHostApplicationBuilder builder, Action<IServiceProvider, DbContextOptionsBuilder> contextBuilder)
        where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contextBuilder);

        builder.Services.AddDbContext<T>((provider, options) =>
        {
            contextBuilder(provider, options);

            if (builder.Environment.IsDevelopment() || builder.Environment.IsTesting())
            {
                options.EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        if (builder.Environment.IsDevelopment() || builder.Environment.IsTesting())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        return builder;
    }
}
