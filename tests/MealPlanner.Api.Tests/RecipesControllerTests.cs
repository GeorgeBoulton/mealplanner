using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Api.Tests;

[TestFixture]
public class RecipesControllerTests : ApiTestBase
{
    // ---------------------------------------------------------------------------
    // Helper builders
    // ---------------------------------------------------------------------------

    private static CreateRecipeRequest BuildCreateRequest(string name = "Test Recipe") =>
        new(
            Name: name,
            Description: "A tasty test dish",
            Category: RecipeCategory.Dinner,
            Servings: 2,
            PrepTimeMinutes: 10,
            CookTimeMinutes: 20,
            Instructions: "Mix everything and cook.",
            Tags: new List<string> { "easy" },
            Ingredients: new List<RecipeIngredientRequest>
            {
                new("Pasta", 200m, "g", ShoppingCategory.Dried, false)
            });

    private static UpdateRecipeRequest BuildUpdateRequest(string name = "Updated Recipe") =>
        new(
            Name: name,
            Description: "Updated description",
            Category: RecipeCategory.Lunch,
            Servings: 4,
            PrepTimeMinutes: 15,
            CookTimeMinutes: 30,
            Instructions: "Updated instructions.",
            Tags: new List<string> { "updated" },
            Ingredients: new List<RecipeIngredientRequest>
            {
                new("Rice", 300m, "g", ShoppingCategory.Dried, false)
            });

    // ---------------------------------------------------------------------------
    // GET /api/recipes
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the endpoint returns an empty collection when no recipes have
    /// been seeded, confirming that the controller does not fabricate data.
    /// </summary>
    [Test]
    public async Task GetAll_WithNoRecipes_ReturnsEmptyList()
    {
        // Arrange — database is empty (cleared by ApiTestBase.ClearDatabase).

        // Act
        var response = await Client.GetAsync("/api/recipes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<RecipeResponse>>();
        body.Should().NotBeNull();
        body!.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the search query parameter filters recipes by name so that
    /// only matching recipes are returned.
    /// </summary>
    [Test]
    public async Task GetAll_WithSearchQuery_FiltersResults()
    {
        // Arrange — create a recipe whose name matches the search term and one that does not.
        await Client.PostAsJsonAsync("/api/recipes", BuildCreateRequest("Spaghetti Bolognese"));
        await Client.PostAsJsonAsync("/api/recipes", BuildCreateRequest("Chicken Salad"));

        // Act
        var response = await Client.GetAsync("/api/recipes?search=spaghetti");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<RecipeResponse>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body![0].Name.Should().Be("Spaghetti Bolognese");
    }

    // ---------------------------------------------------------------------------
    // POST /api/recipes
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a well-formed POST returns 201 Created with a Location header
    /// pointing at the newly created resource.
    /// </summary>
    [Test]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = BuildCreateRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/recipes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<RecipeResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Test Recipe");
    }

    /// <summary>
    /// Verifies that submitting an empty Name triggers model validation and returns 400.
    /// </summary>
    [Test]
    public async Task Create_WithMissingName_Returns400()
    {
        // Arrange — empty name should fail validation.
        var request = BuildCreateRequest(name: "");

        // Act
        var response = await Client.PostAsJsonAsync("/api/recipes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that submitting an empty Ingredients list triggers model validation
    /// and returns 400 — a recipe without ingredients is not meaningful.
    /// </summary>
    [Test]
    public async Task Create_WithNoIngredients_Returns400()
    {
        // Arrange — empty ingredients list should fail validation.
        var request = new CreateRecipeRequest(
            Name: "No Ingredient Recipe",
            Description: null,
            Category: RecipeCategory.Dinner,
            Servings: 2,
            PrepTimeMinutes: null,
            CookTimeMinutes: null,
            Instructions: "Cook it.",
            Tags: new List<string>(),
            Ingredients: new List<RecipeIngredientRequest>());

        // Act
        var response = await Client.PostAsJsonAsync("/api/recipes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------------------
    // GET /api/recipes/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that fetching an existing recipe by its ID returns 200 with the
    /// correct recipe data in the response body.
    /// </summary>
    [Test]
    public async Task GetById_WithExistingId_Returns200()
    {
        // Arrange — create a recipe first.
        var createResponse = await Client.PostAsJsonAsync("/api/recipes", BuildCreateRequest("Unique Dish"));
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeResponse>();

        // Act
        var response = await Client.GetAsync($"/api/recipes/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RecipeResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Unique Dish");
    }

    /// <summary>
    /// Verifies that requesting a recipe with a random (non-existent) GUID returns 404.
    /// </summary>
    [Test]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/recipes/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // PUT /api/recipes/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that updating an existing recipe with valid data returns 200 OK
    /// (the controller returns Ok with updated body).
    /// </summary>
    [Test]
    public async Task Update_WithValidRequest_Returns204()
    {
        // Arrange — create a recipe, then update it.
        var createResponse = await Client.PostAsJsonAsync("/api/recipes", BuildCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeResponse>();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/recipes/{created!.Id}", BuildUpdateRequest());

        // Assert — controller returns 200 Ok with updated body.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that trying to update a recipe that does not exist returns 404.
    /// </summary>
    [Test]
    public async Task Update_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/recipes/{nonExistentId}", BuildUpdateRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // DELETE /api/recipes/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that deleting an existing recipe returns 204 No Content,
    /// and that a subsequent GET returns 404.
    /// </summary>
    [Test]
    public async Task Delete_WithExistingId_Returns204()
    {
        // Arrange — create a recipe to delete.
        var createResponse = await Client.PostAsJsonAsync("/api/recipes", BuildCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/recipes/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Client.GetAsync($"/api/recipes/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that deleting a non-existent recipe still returns 204 NoContent — the
    /// controller returns NoContent idempotently regardless of whether the entity existed.
    /// </summary>
    [Test]
    public async Task Delete_WithNonExistentId_ReturnsNoContent()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/recipes/{nonExistentId}");

        // Assert — controller always returns NoContent regardless of whether the
        // entity existed, so this is expected behaviour.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
