using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPlanner.Application.DTOs;
using MealPlanner.Domain.Enums;

namespace MealPlanner.Api.Tests;

[TestFixture]
public class FridgeControllerTests : ApiTestBase
{
    // ---------------------------------------------------------------------------
    // Helper builders
    // ---------------------------------------------------------------------------

    private static CreateFridgeItemRequest BuildCreateRequest(string name = "Milk") =>
        new(Name: name, Quantity: 1m, Unit: "litre");

    private static UpdateFridgeItemRequest BuildUpdateRequest(string name = "Updated Milk") =>
        new(Name: name, Quantity: 2m, Unit: "litres");

    // ---------------------------------------------------------------------------
    // GET /api/fridge
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that when the fridge is empty the endpoint returns 200 with an
    /// empty list rather than null or an error.
    /// </summary>
    [Test]
    public async Task GetAll_WithNoItems_ReturnsEmptyList()
    {
        // Arrange — database is empty.

        // Act
        var response = await Client.GetAsync("/api/fridge");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<FridgeItemResponse>>();
        body.Should().NotBeNull();
        body!.Should().BeEmpty();
    }

    // ---------------------------------------------------------------------------
    // POST /api/fridge
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a well-formed POST creates a fridge item and returns 201 Created
    /// with a Location header pointing to the new resource.
    /// </summary>
    [Test]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = BuildCreateRequest("Eggs");

        // Act
        var response = await Client.PostAsJsonAsync("/api/fridge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<FridgeItemResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Eggs");
    }

    // ---------------------------------------------------------------------------
    // GET /api/fridge/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an existing fridge item can be retrieved by its ID.
    /// </summary>
    [Test]
    public async Task GetById_WithExistingId_Returns200()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Butter"));
        var created = await createResponse.Content.ReadFromJsonAsync<FridgeItemResponse>();

        // Act
        var response = await Client.GetAsync($"/api/fridge/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FridgeItemResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Butter");
    }

    /// <summary>
    /// Verifies that requesting a fridge item with a random (non-existent) GUID
    /// returns 404.
    /// </summary>
    [Test]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/fridge/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // PUT /api/fridge/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that updating an existing fridge item with valid data returns 200 OK
    /// (controller returns Ok with updated body).
    /// </summary>
    [Test]
    public async Task Update_WithValidRequest_Returns204()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Cheese"));
        var created = await createResponse.Content.ReadFromJsonAsync<FridgeItemResponse>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/fridge/{created!.Id}", BuildUpdateRequest("Cheddar"));

        // Assert — controller returns 200 Ok with updated body.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FridgeItemResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Cheddar");
    }

    // ---------------------------------------------------------------------------
    // DELETE /api/fridge/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that deleting an existing fridge item returns 204 No Content and
    /// that a subsequent GET for the same ID returns 404.
    /// </summary>
    [Test]
    public async Task Delete_WithExistingId_Returns204()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Yoghurt"));
        var created = await createResponse.Content.ReadFromJsonAsync<FridgeItemResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/fridge/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Client.GetAsync($"/api/fridge/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that deleting a fridge item that does not exist still returns 204 NoContent —
    /// the controller returns NoContent idempotently regardless of whether the entity existed.
    /// </summary>
    [Test]
    public async Task Delete_WithNonExistentId_ReturnsNoContent()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/fridge/{nonExistentId}");

        // Assert — controller always returns NoContent regardless of existence.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---------------------------------------------------------------------------
    // DELETE /api/fridge (clear all)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that DELETE /api/fridge removes all fridge items and returns 204 No Content,
    /// with a subsequent GET returning an empty list.
    /// </summary>
    [Test]
    public async Task ClearAll_WithExistingItems_Returns204AndEmptiesFridge()
    {
        // Arrange — add two items so there is something to clear.
        await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Apple"));
        await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Banana"));

        // Act
        var response = await Client.DeleteAsync("/api/fridge");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Client.GetAsync("/api/fridge");
        var body = await getResponse.Content.ReadFromJsonAsync<List<FridgeItemResponse>>();
        body.Should().NotBeNull();
        body!.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that DELETE /api/fridge is idempotent — calling it on an already-empty
    /// fridge still returns 204 No Content.
    /// </summary>
    [Test]
    public async Task ClearAll_WithNoItems_Returns204()
    {
        // Arrange — database is empty.

        // Act
        var response = await Client.DeleteAsync("/api/fridge");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---------------------------------------------------------------------------
    // GET /api/fridge/suggestions
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the suggestions endpoint returns 200 — even with no matching
    /// recipes it must return an empty list rather than an error.
    /// </summary>
    [Test]
    public async Task GetSuggestions_WithFridgeItems_Returns200()
    {
        // Arrange — add a fridge item so the suggestions service has something to work with.
        await Client.PostAsJsonAsync("/api/fridge", BuildCreateRequest("Tomato"));

        // Act
        var response = await Client.GetAsync("/api/fridge/suggestions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<RecipeSuggestionResponse>>();
        body.Should().NotBeNull();
    }
}
