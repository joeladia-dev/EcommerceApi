using System.Net;
using System.Text.Json;
using EcommerceApi.ApiTests.Infrastructure;

namespace EcommerceApi.ApiTests.Common;

public class ErrorContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ErrorContractTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteCategory_WithExistingProducts_ReturnsMiddlewareErrorShape()
    {
        var adminClient = await AuthClientHelper.CreateAuthenticatedClientAsync(_factory, "admin", "password_123");

        var response = await adminClient.DeleteAsync("/api/categories/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("success", out var successProperty));
        Assert.False(successProperty.GetBoolean());
        Assert.True(document.RootElement.TryGetProperty("message", out _));
        Assert.True(document.RootElement.TryGetProperty("errors", out _));
    }
}
