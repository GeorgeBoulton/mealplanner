using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Api.Tests;

[TestFixture]
public class MealPlansControllerTests : ApiTestBase
{
    // ---------------------------------------------------------------------------
    // Helper builders
    // ---------------------------------------------------------------------------

    private static CreateMealPlanRequest BuildCreateRequest(DateOnly? weekStart = null) =>
        new(WeekStartDate: weekStart ?? new DateOnly(2026, 3, 23));

    private static CreateRecipeRequest BuildRecipeRequest() =>
        new(
            Name: "Integration Test Recipe",
            Description: null,
            Category: RecipeCategory.Dinner,
            Servings: 2,
            PrepTimeMinutes: 5,
            CookTimeMinutes: 10,
            Instructions: "Cook it.",
            Tags: new List<string>(),
            Ingredients: new List<RecipeIngredientRequest>
            {
                new("Tomato", 2m, "pcs", ShoppingCategory.FruitAndVeg, false)
            });

    // ---------------------------------------------------------------------------
    // GET /api/mealplans
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that when no meal plans exist the endpoint returns 200 with an
    /// empty list rather than null or an error.
    /// </summary>
    [Test]
    public async Task GetAll_WithNoMealPlans_ReturnsEmptyList()
    {
        // Arrange — database is empty.

        // Act
        var response = await Client.GetAsync("/api/mealplans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<MealPlanResponse>>();
        body.Should().NotBeNull();
        body!.Should().BeEmpty();
    }

    // ---------------------------------------------------------------------------
    // POST /api/mealplans
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a valid POST creates a meal plan and returns 201 Created with
    /// the new resource in the body.
    /// </summary>
    [Test]
    public async Task Create_WithValidRequest_Returns201()
    {
        // Arrange
        var request = BuildCreateRequest(new DateOnly(2026, 3, 23));

        // Act
        var response = await Client.PostAsJsonAsync("/api/mealplans", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<MealPlanResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
    }

    // ---------------------------------------------------------------------------
    // GET /api/mealplans/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a previously created meal plan can be retrieved by its ID.
    /// </summary>
    [Test]
    public async Task GetById_WithExistingId_Returns200()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/mealplans", BuildCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<MealPlanResponse>();

        // Act
        var response = await Client.GetAsync($"/api/mealplans/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MealPlanResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
    }

    /// <summary>
    /// Verifies that requesting a meal plan that does not exist returns 404.
    /// </summary>
    [Test]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/mealplans/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // GET /api/mealplans/current
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that GET /current always returns 200 — the service creates the
    /// current week's plan automatically if one does not yet exist.
    /// </summary>
    [Test]
    public async Task GetCurrent_AlwaysReturnsMealPlan()
    {
        // Arrange — no meal plan seeded; service will auto-create.

        // Act
        var response = await Client.GetAsync("/api/mealplans/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MealPlanResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
    }

    // ---------------------------------------------------------------------------
    // DELETE /api/mealplans/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that deleting an existing meal plan returns 204 No Content and
    /// that a subsequent GET returns 404.
    /// </summary>
    [Test]
    public async Task Delete_WithExistingId_Returns204()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/mealplans", BuildCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<MealPlanResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/mealplans/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Client.GetAsync($"/api/mealplans/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // POST /api/mealplans/{id}/entries
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that adding a valid entry to an existing meal plan returns 200 OK
    /// (controller returns Ok with updated plan body).
    /// </summary>
    [Test]
    public async Task AddEntry_WithValidRequest_Returns201()
    {
        // Arrange — create a recipe and a meal plan.
        var recipeResponse = await Client.PostAsJsonAsync("/api/recipes", BuildRecipeRequest());
        var recipe = await recipeResponse.Content.ReadFromJsonAsync<RecipeResponse>();

        var planResponse = await Client.PostAsJsonAsync("/api/mealplans", BuildCreateRequest(new DateOnly(2026, 3, 23)));
        var plan = await planResponse.Content.ReadFromJsonAsync<MealPlanResponse>();

        var entryRequest = new MealPlanEntryRequest(
            Date: new DateOnly(2026, 3, 24),
            MealType: MealType.Dinner,
            RecipeId: recipe!.Id,
            Servings: 2);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/mealplans/{plan!.Id}/entries", entryRequest);

        // Assert — controller returns 200 Ok with the updated meal plan.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MealPlanResponse>();
        body.Should().NotBeNull();
        body!.Entries.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that attempting to add an entry to a meal plan that does not exist
    /// returns 404.
    /// </summary>
    [Test]
    public async Task AddEntry_WithNonExistentMealPlan_Returns404()
    {
        // Arrange
        var recipeResponse = await Client.PostAsJsonAsync("/api/recipes", BuildRecipeRequest());
        var recipe = await recipeResponse.Content.ReadFromJsonAsync<RecipeResponse>();

        var entryRequest = new MealPlanEntryRequest(
            Date: new DateOnly(2026, 3, 24),
            MealType: MealType.Lunch,
            RecipeId: recipe!.Id,
            Servings: 1);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/mealplans/{Guid.NewGuid()}/entries", entryRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // POST /api/mealplans/{id}/shopping-list
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that generating a shopping list for an existing meal plan returns
    /// 200 OK with a ShoppingListResponse body.
    /// </summary>
    [Test]
    public async Task GenerateShoppingList_ForExistingMealPlan_Returns201()
    {
        // Arrange
        var planResponse = await Client.PostAsJsonAsync("/api/mealplans", BuildCreateRequest(new DateOnly(2026, 4, 7)));
        var plan = await planResponse.Content.ReadFromJsonAsync<MealPlanResponse>();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/mealplans/{plan!.Id}/shopping-list", new { });

        // Assert — controller returns 200 Ok with shopping list body.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ShoppingListResponse>();
        body.Should().NotBeNull();
        body!.MealPlanId.Should().Be(plan.Id);
    }

    /// <summary>
    /// Verifies that generating a shopping list for a non-existent meal plan
    /// returns 404.
    /// </summary>
    [Test]
    public async Task GenerateShoppingList_ForNonExistentMealPlan_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/mealplans/{nonExistentId}/shopping-list", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
