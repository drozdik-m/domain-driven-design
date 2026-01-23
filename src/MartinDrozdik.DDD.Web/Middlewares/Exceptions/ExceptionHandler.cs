using System.Diagnostics;
using MartinDrozdik.DDD.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace MartinDrozdik.DDD.Web.Middlewares.Exceptions;

/// <summary>
/// Base class for exception handlers.
/// </summary>
public abstract class ExceptionHandler : IExceptionHandler
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
    /// Construct extension data for error response.
    /// Includes business details if the exception is: <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="exception">The exception to get details from, if any.</param>
    /// <returns>Dictionary of key-value data related to the request and the <paramref name="exception"/>.</returns>
    protected static IDictionary<string, object?> GetExtensionDataWithDetails(Exception? exception)
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
            if (extensionData.ContainsKey(key))
            {
                extensionData[key + Guid.NewGuid().ToString()] = value;
            }
            else
            {
                extensionData[key] = value;
            }
        }

        return extensionData;
    }
}
