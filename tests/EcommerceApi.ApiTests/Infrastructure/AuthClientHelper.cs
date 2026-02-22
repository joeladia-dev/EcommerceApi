using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcommerceApi.ApiTests.Contracts;

namespace EcommerceApi.ApiTests.Infrastructure;

public static class AuthClientHelper
{
    public static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        if (payload?.Data?.Token is null)
        {
            throw new InvalidOperationException("Login response did not contain a JWT token.");
        }

        return payload.Data.Token;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string username,
        string password)
    {
        var client = factory.CreateApiClient();
        var token = await GetTokenAsync(client, username, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
