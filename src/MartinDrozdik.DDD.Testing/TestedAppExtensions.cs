using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using MartinDrozdik.DDD.Testing.Attributes;
using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Extensions for <see cref="TestedApp{TProgram}"/>.
/// </summary>
public static class TestedAppExtensions
{
    public static async Task<RequestResult<T>> GetJsonAsync<T>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var response = await client.GetAsync(requestUri, cancellationToken);
        return await ProcessModelResponse<T>(testedApp, response, cancellationToken);
    }

    public static async Task<RequestResult> DeleteAsync(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var response = await client.DeleteAsync(requestUri, cancellationToken);
        return ProcessResponse(testedApp, response);
    }

    public static async Task<RequestResult<T>> DeleteJsonAsync<T>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var response = await client.DeleteAsync(requestUri, cancellationToken);
        return await ProcessModelResponse<T>(testedApp, response, cancellationToken);
    }

    public static async Task<RequestResult> PostJsonAsync<T>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T payload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await client.PostAsync(requestUri, content, cancellationToken);
        return ProcessResponse(testedApp, response);
    }

    public static async Task<RequestResult<S>> PostJsonWithResponseAsync<T, S>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T payload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await client.PostAsync(requestUri, content, cancellationToken);
        return await ProcessModelResponse<S>(testedApp, response, cancellationToken);
    }

    public static async Task<RequestResult> PutJsonAsync<T>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T payload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await client.PutAsync(requestUri, content, cancellationToken);
        return ProcessResponse(testedApp, response);
    }

    public static async Task<RequestResult<S>> PutJsonWithResponseAsync<T, S>(this ITestedApp testedApp, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T payload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = testedApp.CreateClient();

        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await client.PutAsync(requestUri, content, cancellationToken);
        return await ProcessModelResponse<S>(testedApp, response, cancellationToken);
    }

    private static RequestResult ProcessResponse(ITestedApp testedApp, HttpResponseMessage response)
    {
        LogResponse(testedApp.TestOutputHelper, response);
        return RequestResult.Create(response);
    }

    private static async Task<RequestResult<T>> ProcessModelResponse<T>(ITestedApp testedApp, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        LogResponse(testedApp.TestOutputHelper, response, responseContent);

        if (response.IsSuccessStatusCode)
        {
            var responseModel = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerOptions.Web);
            if (responseModel is null)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                Assert.Fail($"Failed to parse response content as JSON of type {typeof(T).FullName}. Content: {content}");
                return RequestResult<T>.Failure(response);
            }

            return RequestResult<T>.Success(responseModel, response);
        }

        return RequestResult<T>.Failure(response);
    }

    private static void LogResponse(ITestOutputHelper outputHelper, HttpResponseMessage response, string responseContent)
    {
        outputHelper.WriteLine($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}){Environment.NewLine}{responseContent}");
    }

    private static void LogResponse(ITestOutputHelper outputHelper, HttpResponseMessage response)
    {
        outputHelper.WriteLine($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
    }
}

/// <summary>
/// Represents the result of an HTTP request, encapsulating both the parsed value (if successful) and the original HTTP response message for further inspection.
/// </summary>
/// <typeparam name="T">Type of the parsed model.</typeparam>
public readonly struct RequestResult<T>
{
    private readonly T? _value;

    private RequestResult(T? value, HttpResponseMessage response)
    {
        Response = response;
        _value = value;
    }

    /// <summary>
    /// Gets the original HTTP response message, regardless of the success of the operation. This allows access to status codes, headers, and raw content for both successful and failed requests.
    /// </summary>
    public readonly HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets the parse result value. Non-null when <see cref="IsSuccess"/> is true.
    /// </summary>
    public readonly T Value => _value is not null
        ? _value
        : throw new InvalidOperationException("Request was not successful.");

    /// <summary>
    /// Gets a value indicating whether the operation succeeded and <see cref="Value"/> is available.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => _value is not null;

    /// <summary>
    /// Creates a successful <see cref="RequestResult{T}"/> with the provided value and HTTP response message.
    /// </summary>
    /// <param name="value">The parsed model instance.</param>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult{T}"/> representing a successful operation.</returns>
    public static RequestResult<T> Success(T value, HttpResponseMessage responseMessage) => new(value, responseMessage);

    /// <summary>
    /// Creates a failed <see cref="RequestResult{T}"/> with the provided HTTP response message.
    /// </summary>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult{T}"/> representing a failed operation.</returns>
    public static RequestResult<T> Failure(HttpResponseMessage responseMessage) => new(default, responseMessage);

    /// <summary>
    /// Asserts that the request was successful.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public async Task EnsureSuccessAsync()
    {
        if (!IsSuccess)
        {
            var content = await Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected successful response but got status code {(int)Response.StatusCode} ({Response.ReasonPhrase}). Content: {content}");
        }
    }
}

/// <summary>
/// Represents the result of an HTTP request, encapsulating bthe original HTTP response message for further inspection.
/// </summary>
public readonly struct RequestResult
{
    private RequestResult(HttpResponseMessage response)
    {
        Response = response;
    }

    /// <summary>
    /// Gets the original HTTP response message, regardless of the success of the operation. This allows access to status codes, headers, and raw content for both successful and failed requests.
    /// </summary>
    public readonly HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Response.IsSuccessStatusCode;

    /// <summary>
    /// Creates a successful <see cref="RequestResult"/> with the HTTP response message.
    /// </summary>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult"/> representing a successful operation.</returns>
    public static RequestResult Create(HttpResponseMessage responseMessage) => new(responseMessage);

    /// <summary>
    /// Asserts that the request was successful.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [AssertionMethod]
    public async Task EnsureSuccessAsync()
    {
        if (!IsSuccess)
        {
            var content = await Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected successful response but got status code {(int)Response.StatusCode} ({Response.ReasonPhrase}). Content: {content}");
        }
    }
}
