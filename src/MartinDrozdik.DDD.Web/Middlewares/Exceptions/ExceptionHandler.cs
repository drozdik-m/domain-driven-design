using System.Diagnostics;
using MartinDrozdik.DDD.Exceptions;
using MartinDrozdik.DDD.Extensions;
using MartinDrozdik.DDD.Web.Environments;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Base class for exception handlers.
/// </summary>
public abstract class ExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    /// <inheritdoc />
    public abstract ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken);

    /// <summary>
    /// Construct extension data for error response.
    /// Includes trace identifier.
    /// </summary>
    /// <returns>Dictionary of key-value data related to the request.</returns>
    protected static IDictionary<string, object?> GetExtensionData()
    {
        var traceId = Activity.Current?.TraceId;

        var extensionData = new Dictionary<string, object?>();
        if (traceId is not null)
        {
            extensionData["traceId"] = traceId.ToString();
        }

        return extensionData;
    }

    /// <summary>
    /// Gets error detail. Includes full exception message.
    /// </summary>
    /// <param name="exception">The exception to get detail from.</param>
    /// <returns>Error detail string.</returns>
    protected static string GetExceptionDetail(Exception exception)
    {
        return exception.Message;
    }

    /// <summary>
    /// Construct extension data for error response.
    /// Includes business details if the exception is: <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="exception">The exception to get details from, if any.</param>
    /// <returns>Dictionary of key-value data related to the request and the <paramref name="exception"/>.</returns>
    protected IDictionary<string, object?> GetExtensionDataWithDetails(Exception? exception)
    {
        var extensionData = GetExtensionData();

        // Try get details from BusinessRuleException
        var detailsDictionary = exception switch
        {
            BusinessRuleException businessRuleException => businessRuleException.DetailsDictionary,
            _ => new Dictionary<string, string[]>()
        };

        foreach (var (key, value) in detailsDictionary)
        {
            var updatedKey = key.FirstToLower();
            if (extensionData.ContainsKey(updatedKey))
            {
                extensionData[updatedKey + Guid.NewGuid().ToString()] = value;
            }
            else
            {
                extensionData[updatedKey] = value;
            }
        }

        // In development environment, also add exception message
        if (exception is not null && environment.IsDevelopment())
        {
            extensionData["exception"] = exception.ToString();
        }

        return extensionData;
    }
}
