using System.Net;
using System.Net.Http.Json;
using EcommerceApi.ApiTests.Contracts;
using EcommerceApi.ApiTests.Infrastructure;

namespace EcommerceApi.ApiTests.Categories;

public class CategoriesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoriesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededCategories()
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.NotEmpty(payload.Data!);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/categories", new
        {
            name = "Gaming"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithIdMismatch_ReturnsBadRequest()
    {
        var adminClient = await AuthClientHelper.CreateAuthenticatedClientAsync(_factory, "admin", "password_123");

        var response = await adminClient.PutAsJsonAsync("/api/categories/1", new
        {
            id = 2,
            name = "Mismatch"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
    }

    [Fact]
    public async Task Delete_AsNonAdminUser_ReturnsForbidden()
    {
        var userClient = await AuthClientHelper.CreateAuthenticatedClientAsync(_factory, "user", "password_321");

        var response = await userClient.DeleteAsync("/api/categories/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProductsForMissingCategory_ReturnsNotFound()
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/categories/999999/products");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
