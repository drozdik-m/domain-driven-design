using System.Diagnostics;
using MartinDrozdik.DDD.Models.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MartinDrozdik.DDD.Demo.Middlewares.Exceptions;

public abstract class ExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public abstract ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken);

    /// <summary>
    /// Construct extension data for error response.
    /// </summary>
    protected IDictionary<string, object?> GetExtensionData(HttpContext context, Exception? exception)
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
    /// Includes business details
    /// </summary>
    protected IDictionary<string, object?> GetExtensionDataWithDetails(HttpContext context, Exception? exception)
    {
        var extensionData = GetExtensionData(context, exception);

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
