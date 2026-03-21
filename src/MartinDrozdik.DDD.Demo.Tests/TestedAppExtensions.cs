using MartinDrozdik.DDD.Demo.Client.Generated;
using MartinDrozdik.DDD.Testing;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace MartinDrozdik.DDD.Demo.Tests;

public static class TestedAppExtensions
{
    public static DddClient CreateDddClient(this TestedApp<Program> factory)
    {
        var httpClient = factory.CreateClient();
        var authProvider = new AnonymousAuthenticationProvider();
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = httpClient.BaseAddress?.ToString()
                ?? throw new InvalidOperationException("HttpClient BaseAddress is null"),
        };

        var client = new DddClient(requestAdapter);
        return client;
    }
}
