using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CloudWatcher.Data;
using CloudWatcher.Tests.Fixtures;
using CloudWatcher.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CloudWatcher.Tests.Http;

/// <summary>
/// Live HTTP tests for GET /api/v2/inventory/{partId} endpoint.
/// Tests the actual HTTP interface against WebApplicationFactory test server.
/// </summary>
public class InventoryGetPartByIdHttpTests : IntegrationTestBase
{
    public InventoryGetPartByIdHttpTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPartById_WithValidPartId_Returns200WithCompleteResponse()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateSimplePart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        // Verify JSON structure
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("partId", out _));
        Assert.True(root.TryGetProperty("code", out _));
        Assert.True(root.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task GetPartById_WithInvalidUUID_Returns400BadRequest()
    {
        // Arrange
        SetAuthorizationToken();
        var invalidId = "not-a-uuid";

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetPartById_WithNonexistentPartId_Returns404NotFound()
    {
        // Arrange
        SetAuthorizationToken();
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{nonexistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetPartById_WithoutAuthorization_Returns401Unauthorized()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateSimplePart(context);
        // Don't set authorization token
        
        // Need to create a new client without the auth header
        var unauthorizedClient = Factory.CreateClient();

        // Act
        var response = await unauthorizedClient.GetAsync($"/api/v2/inventory/{part.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPartById_ResponseStructure_ContainsRequiredFields()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateComplexPart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Assert - Verify all expected fields are present
        Assert.True(root.TryGetProperty("partId", out _), "Missing 'partId'");
        Assert.True(root.TryGetProperty("code", out _), "Missing 'code'");
        Assert.True(root.TryGetProperty("name", out _), "Missing 'name'");
        Assert.True(root.TryGetProperty("description", out _), "Missing 'description'");
        Assert.True(root.TryGetProperty("category", out _), "Missing 'category'");
        Assert.True(root.TryGetProperty("standardPrice", out _), "Missing 'standardPrice'");
        Assert.True(root.TryGetProperty("variants", out var variantsProp), "Missing 'variants'");
        Assert.True(variantsProp.ValueKind == JsonValueKind.Array, "'variants' should be an array");
        Assert.True(root.TryGetProperty("totalInventory", out _), "Missing 'totalInventory'");
    }

    [Fact]
    public async Task GetPartById_WithMultipleVariants_IncludesAllVariants()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateComplexPart(context); // 5 variants
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("variants", out var variantsArray));
        Assert.Equal(5, variantsArray.GetArrayLength());
    }

    [Fact]
    public async Task GetPartById_VariantResponse_ContainsExpectedFields()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateSimplePart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("variants", out var variantsArray));
        Assert.True(variantsArray.GetArrayLength() > 0);

        var firstVariant = variantsArray[0];
        Assert.True(firstVariant.TryGetProperty("variantId", out _), "Missing 'variantId'");
        Assert.True(firstVariant.TryGetProperty("variantCode", out _), "Missing 'variantCode'");
        Assert.True(firstVariant.TryGetProperty("attributes", out _), "Missing 'attributes'");
        Assert.True(firstVariant.TryGetProperty("variantPrice", out _), "Missing 'variantPrice'");
        Assert.True(firstVariant.TryGetProperty("inventory", out _), "Missing 'inventory'");
    }

    [Fact]
    public async Task GetPartById_InventoryItems_ContainLocationAndQuantity()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateSimplePart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("variants", out var variantsArray));
        var firstVariant = variantsArray[0];
        Assert.True(firstVariant.TryGetProperty("inventory", out var inventoryArray));
        Assert.True(inventoryArray.GetArrayLength() > 0);

        var firstInventory = inventoryArray[0];
        Assert.True(firstInventory.TryGetProperty("locationId", out _), "Missing 'locationId'");
        Assert.True(firstInventory.TryGetProperty("locationName", out _), "Missing 'locationName'");
        Assert.True(firstInventory.TryGetProperty("quantityOnHand", out _), "Missing 'quantityOnHand'");
        Assert.True(firstInventory.TryGetProperty("reorderLevel", out _), "Missing 'reorderLevel'");
        Assert.True(firstInventory.TryGetProperty("isLowStock", out _), "Missing 'isLowStock'");
    }

    [Fact]
    public async Task GetPartById_LowStockIndicator_IsTrueWhenBelowThreshold()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateLowStockPart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("variants", out var variantsArray));
        var hasLowStockIndicator = false;
        
        foreach (JsonElement variant in variantsArray.EnumerateArray())
        {
            if (variant.TryGetProperty("inventory", out var inventoryArray))
            {
                foreach (JsonElement item in inventoryArray.EnumerateArray())
                {
                    if (item.TryGetProperty("isLowStock", out var isLowStockProp))
                    {
                        if (isLowStockProp.GetBoolean())
                        {
                            hasLowStockIndicator = true;
                            break;
                        }
                    }
                }
            }
        }
        
        Assert.True(hasLowStockIndicator, "Expected at least one low stock indicator");
    }

    [Fact]
    public async Task GetPartById_ValidResponse_DeserializesToValidJson()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();
        var part = TestDataFactory.CreateComplexPart(context);
        SetAuthorizationToken();

        // Act
        var response = await Client.GetAsync($"/api/v2/inventory/{part.Id}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Should not throw
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }
}
