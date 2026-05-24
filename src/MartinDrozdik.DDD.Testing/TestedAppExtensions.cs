using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using MartinDrozdik.DDD.Testing.Attributes;
using Xunit;

namespace MartinDrozdik.DDD.Testing;

/// <summary>
/// Extensions for <see cref="TestedApp{TProgram}"/>.
/// Because manually wiring up HttpClient in every test is a special kind of suffering.
/// </summary>
public static class TestedAppExtensions
{
    private static CancellationToken CurrentToken => TestContext.Current.CancellationToken;

    /// <summary>
    /// Sends a GET request to the specified URI and attempts to parse the response body as JSON into the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of the response model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult<T>> GetJsonAsync<T>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var response = await testedApp.CreateClient().GetAsync(requestUri, CurrentToken);
        return await ProcessModelResponse<T>(testedApp, response, CurrentToken);
    }

    /// <summary>
    /// Sends a DELETE request to the specified URI and returns the result.
    /// </summary>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult> DeleteAsync(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var response = await testedApp.CreateClient().DeleteAsync(requestUri, CurrentToken);
        return ProcessResponse(testedApp, response);
    }

    /// <summary>
    /// Sends a DELETE request to the specified URI and attempts to parse the response body as JSON into the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of the response model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult<T>> DeleteJsonAsync<T>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        var response = await testedApp.CreateClient().DeleteAsync(requestUri, CurrentToken);
        return await ProcessModelResponse<T>(testedApp, response, CurrentToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON body to the specified URI and returns the result.
    /// </summary>
    /// <typeparam name="T">Type of the request model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <param name="payload">The JSON payload for the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult> PostJsonAsync<T>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
        T payload)
    {
        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await testedApp.CreateClient().PostAsync(requestUri, content, CurrentToken);
        return ProcessResponse(testedApp, response);
    }

    /// <summary>
    /// Sends a POST request with a JSON body to the specified URI and attempts to parse the response body as JSON into the specified type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request model.</typeparam>
    /// <typeparam name="TResponse">Type of the response model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <param name="payload">The JSON payload for the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult<TResponse>> PostJsonWithResponseAsync<TRequest, TResponse>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
        TRequest payload)
    {
        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await testedApp.CreateClient().PostAsync(requestUri, content, CurrentToken);
        return await ProcessModelResponse<TResponse>(testedApp, response, CurrentToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body to the specified URI and returns the result.
    /// </summary>
    /// <typeparam name="T">Type of the request model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <param name="payload">The JSON payload for the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult> PutJsonAsync<T>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
        T payload)
    {
        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await testedApp.CreateClient().PutAsync(requestUri, content, CurrentToken);
        return ProcessResponse(testedApp, response);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body to the specified URI and attempts to parse the response body as JSON into the specified type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request model.</typeparam>
    /// <typeparam name="TResponse">Type of the response model.</typeparam>
    /// <param name="testedApp">The tested application.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <param name="payload">The JSON payload for the request.</param>
    /// <returns>The result of the request.</returns>
    public static async Task<RequestResult<TResponse>> PutJsonWithResponseAsync<TRequest, TResponse>(
        this ITestedApp testedApp,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri,
        TRequest payload)
    {
        var content = JsonContent.Create(payload, options: JsonSerializerOptions.Web);
        var response = await testedApp.CreateClient().PutAsync(requestUri, content, CurrentToken);
        return await ProcessModelResponse<TResponse>(testedApp, response, CurrentToken);
    }

    private static RequestResult ProcessResponse(ITestedApp testedApp, HttpResponseMessage response)
    {
        LogResponse(testedApp.TestOutputHelper, response);
        return RequestResult.Create(response);
    }

    private static async Task<RequestResult<T>> ProcessModelResponse<T>(
        ITestedApp testedApp,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        LogResponse(testedApp.TestOutputHelper, response, responseContent);

        if (!response.IsSuccessStatusCode)
        {
            return RequestResult<T>.Failure(response);
        }

        var responseModel = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerOptions.Web);
        if (responseModel is null)
        {
            Assert.Fail($"Failed to parse response content as JSON of type {typeof(T).FullName}. Content: {responseContent}");
        }

        return RequestResult<T>.Success(responseModel!, response);
    }

    private static void LogResponse(
        ITestOutputHelper outputHelper,
        HttpResponseMessage response,
        string? responseContent = null)
    {
        var body = responseContent is not null
            ? Environment.NewLine + responseContent
            : string.Empty;

        outputHelper.WriteLine($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}){body}");
    }
}

/// <summary>
/// Represents the result of an HTTP request, bundling the parsed value (if successful)
/// and the original HTTP response for further inspection.
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
    /// Gets the original HTTP response message, regardless of whether the request succeeded.
    /// Useful for inspecting status codes, headers, and raw content.
    /// </summary>
    public readonly HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets the parsed response value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public readonly T Value => _value is not null
        ? _value
        : throw new InvalidOperationException("Request was not successful.");

    /// <summary>
    /// Gets a value indicating whether the request succeeded and <see cref="Value"/> is available.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => _value is not null;

    /// <summary>
    /// Creates a successful <see cref="RequestResult{T}"/> with the provided value and HTTP response.
    /// </summary>
    /// <param name="value">The parsed model instance.</param>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult{T}"/> representing a successful operation.</returns>
    public static RequestResult<T> Success(T value, HttpResponseMessage responseMessage) => new(value, responseMessage);

    /// <summary>
    /// Creates a failed <see cref="RequestResult{T}"/> with the provided HTTP response.
    /// </summary>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult{T}"/> representing a failed operation.</returns>
    public static RequestResult<T> Failure(HttpResponseMessage responseMessage) => new(default, responseMessage);

    /// <summary>
    /// Asserts that the request was successful. Fails the test with a helpful message if it wasn't.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public async Task EnsureSuccessAsync()
    {
        if (IsSuccess)
        {
            return;
        }

        var content = await Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Fail($"Expected successful response but got {(int)Response.StatusCode} ({Response.ReasonPhrase}). Content: {content}");
    }
}

/// <summary>
/// Represents the result of an HTTP request with no response body.
/// </summary>
public readonly struct RequestResult
{
    private RequestResult(HttpResponseMessage response)
    {
        Response = response;
    }

    /// <summary>
    /// Gets the original HTTP response message.
    /// </summary>
    public readonly HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets a value indicating whether the request succeeded.
    /// </summary>
    public bool IsSuccess => Response.IsSuccessStatusCode;

    /// <summary>
    /// Creates a <see cref="RequestResult"/> wrapping the provided HTTP response.
    /// </summary>
    /// <param name="responseMessage">The original HTTP response message.</param>
    /// <returns> A new instance of <see cref="RequestResult"/> representing a successful operation.</returns>
    public static RequestResult Create(HttpResponseMessage responseMessage) => new(responseMessage);

    /// <summary>
    /// Asserts that the request was successful. Fails the test with a helpful message if it wasn't.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [AssertionMethod]
    public async Task EnsureSuccessAsync()
    {
        if (IsSuccess)
        {
            return;
        }

        var content = await Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Fail($"Expected successful response but got {(int)Response.StatusCode} ({Response.ReasonPhrase}). Content: {content}");
    }
}
