using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPlanner.Application.DTOs;
using MealPlanner.Domain.Enums;

namespace MealPlanner.Api.Tests;

[TestFixture]
public class ShoppingListsControllerTests : ApiTestBase
{
    // ---------------------------------------------------------------------------
    // Helper builders
    // ---------------------------------------------------------------------------

    private static CreateRecipeRequest BuildRecipeRequest() =>
        new(
            Name: "Shopping List Test Recipe",
            Description: null,
            Category: RecipeCategory.Dinner,
            Servings: 2,
            PrepTimeMinutes: 10,
            CookTimeMinutes: 20,
            Instructions: "Cook it.",
            Tags: new List<string>(),
            Ingredients: new List<RecipeIngredientRequest>
            {
                new("Onion", 1m, "pcs", ShoppingCategory.FruitAndVeg, false)
            });

    /// <summary>
    /// Creates a meal plan, optionally adds an entry, then generates a shopping list
    /// and returns the ShoppingListResponse.
    /// </summary>
    private async Task<ShoppingListResponse> CreateShoppingListAsync(bool withEntry = true)
    {
        var weekStart = new DateOnly(2026, 3, 23);
        var planResponse = await Client.PostAsJsonAsync("/api/mealplans",
            new CreateMealPlanRequest(WeekStartDate: weekStart));
        var plan = await planResponse.Content.ReadFromJsonAsync<MealPlanResponse>();

        if (withEntry)
        {
            var recipeResponse = await Client.PostAsJsonAsync("/api/recipes", BuildRecipeRequest());
            var recipe = await recipeResponse.Content.ReadFromJsonAsync<RecipeResponse>();

            var entryRequest = new MealPlanEntryRequest(
                Date: new DateOnly(2026, 3, 24),
                MealType: MealType.Dinner,
                RecipeId: recipe!.Id,
                Servings: 2);
            await Client.PostAsJsonAsync($"/api/mealplans/{plan!.Id}/entries", entryRequest);
        }

        var slResponse = await Client.PostAsJsonAsync($"/api/mealplans/{plan!.Id}/shopping-list", new { });
        return (await slResponse.Content.ReadFromJsonAsync<ShoppingListResponse>())!;
    }

    // ---------------------------------------------------------------------------
    // GET /api/shopping-lists/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that fetching a shopping list by its ID returns 200 with the
    /// correct body — confirms the resource was persisted and is retrievable.
    /// </summary>
    [Test]
    public async Task GetById_WithExistingId_Returns200()
    {
        // Arrange
        var shoppingList = await CreateShoppingListAsync();

        // Act
        var response = await Client.GetAsync($"/api/shopping-lists/{shoppingList.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ShoppingListResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(shoppingList.Id);
    }

    /// <summary>
    /// Verifies that requesting a shopping list with a random (non-existent) GUID
    /// returns 404.
    /// </summary>
    [Test]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/shopping-lists/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------------------------------------------------------------------
    // DELETE /api/shopping-lists/{id}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that deleting an existing shopping list returns 204 No Content and
    /// that a subsequent GET returns 404.
    /// </summary>
    [Test]
    public async Task Delete_WithExistingId_Returns204()
    {
        // Arrange
        var shoppingList = await CreateShoppingListAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/shopping-lists/{shoppingList.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await Client.GetAsync($"/api/shopping-lists/{shoppingList.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that deleting a non-existent shopping list still returns 204 NoContent —
    /// the controller returns NoContent idempotently regardless of whether the entity existed.
    /// </summary>
    [Test]
    public async Task Delete_WithNonExistentId_ReturnsNoContent()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/shopping-lists/{nonExistentId}");

        // Assert — controller always returns NoContent regardless of existence.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---------------------------------------------------------------------------
    // PUT /api/shopping-lists/{id}/items/{itemId}
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that toggling the IsChecked flag on a shopping list item returns 200
    /// OK with the updated shopping list body.
    /// </summary>
    [Test]
    public async Task UpdateItem_ToggleChecked_Returns204()
    {
        // Arrange — generate a list that contains at least one item.
        var shoppingList = await CreateShoppingListAsync(withEntry: true);
        shoppingList.Items.Should().NotBeEmpty("shopping list must have items to toggle");
        var firstItem = shoppingList.Items[0];

        var updateRequest = new UpdateShoppingListItemRequest(IsChecked: true);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/shopping-lists/{shoppingList.Id}/items/{firstItem.Id}",
            updateRequest);

        // Assert — controller returns 200 Ok with updated body.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ShoppingListResponse>();
        body.Should().NotBeNull();
        var updatedItem = body!.Items.Single(i => i.Id == firstItem.Id);
        updatedItem.IsChecked.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // GET /api/shopping-lists/{id}/export
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Verifies that exporting an existing shopping list returns 200 with a
    /// text/plain content type — the export is a plain-text formatted list.
    /// </summary>
    [Test]
    public async Task Export_WithExistingId_ReturnsPlainText()
    {
        // Arrange
        var shoppingList = await CreateShoppingListAsync(withEntry: true);

        // Act
        var response = await Client.GetAsync($"/api/shopping-lists/{shoppingList.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().NotBeNullOrWhiteSpace();
    }
}
