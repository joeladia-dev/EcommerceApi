using System.Net;
using System.Net.Http.Json;
using EcommerceApi.ApiTests.Contracts;
using EcommerceApi.ApiTests.Infrastructure;

namespace EcommerceApi.ApiTests.Products;

public class ProductsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsPagedProducts()
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductDto>>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.NotEmpty(payload.Data!);
        Assert.True(payload.Data!.Count <= 20);
    }

    [Fact]
    public async Task GetAll_WithSearchFilter_ReturnsMatchingProducts()
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/products?search=Mouse&sort=name");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductDto>>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.All(payload.Data!, product => Assert.Contains("Mouse", product.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Test Product",
            description = "Test Description",
            price = 10.5m,
            stock = 3,
            categoryId = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidCategory_ReturnsBadRequest()
    {
        var adminClient = await AuthClientHelper.CreateAuthenticatedClientAsync(_factory, "admin", "password_123");

        var response = await adminClient.PostAsJsonAsync("/api/products", new
        {
            name = "Test Product",
            description = "Test Description",
            price = 10.5m,
            stock = 3,
            categoryId = 999999
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
    }

    [Fact]
    public async Task Delete_AsNonAdminUser_ReturnsForbidden()
    {
        var userClient = await AuthClientHelper.CreateAuthenticatedClientAsync(_factory, "user", "password_321");

        var response = await userClient.DeleteAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
