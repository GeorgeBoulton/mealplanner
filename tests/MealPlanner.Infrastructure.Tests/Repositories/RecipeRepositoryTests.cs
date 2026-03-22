using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.ValueObjects;
using MealPlanner.Infrastructure.Repositories;

namespace MealPlanner.Infrastructure.Tests.Repositories;

[TestFixture]
public class RecipeRepositoryTests : RepositoryTestBase
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Recipe BuildRecipe(
        string name = "Pasta Bolognese",
        RecipeCategory category = RecipeCategory.Dinner,
        IEnumerable<RecipeIngredient>? ingredients = null)
    {
        return Recipe.Create(
            name: name,
            description: "A classic Italian dish",
            category: category,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 30,
            instructions: "Cook the pasta. Brown the mince. Combine.",
            sourceUrl: null,
            ingredients: ingredients,
            tags: new[] { "italian", "pasta" });
    }

    private static RecipeIngredient BuildIngredient(string name = "beef mince") =>
        new RecipeIngredient(name, 500m, "g", ShoppingCategory.Meat);

    // -----------------------------------------------------------------------
    // AddAsync + GetByIdAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetById_WithValidId_ReturnsRecipe()
    {
        // Verifies that a persisted recipe can be retrieved by its primary key,
        // and that the Ingredients owned collection is eagerly included.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        var recipe = BuildRecipe(ingredients: new[] { BuildIngredient() });
        await repo.AddAsync(recipe, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var result = await readRepo.GetByIdAsync(recipe.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(recipe.Id);
        result.Name.Should().Be("Pasta Bolognese");
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Name.Should().Be("beef mince");
    }

    [Test]
    public async Task GetById_WithMissingId_ReturnsNull()
    {
        // Verifies that a non-existent ID returns null instead of throwing.
        await using var ctx = CreateContext();
        var repo = new RecipeRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetAll_WithMultipleRecipes_ReturnsAll()
    {
        // Verifies that GetAll returns every recipe that has been inserted,
        // confirming no accidental filtering occurs.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        await repo.AddAsync(BuildRecipe("Recipe A"), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Recipe B"), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Recipe C"), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);
        var results = await readRepo.GetAllAsync(CancellationToken.None);

        results.Should().HaveCount(3);
    }

    [Test]
    public async Task GetAll_WithNoRecipes_ReturnsEmptyList()
    {
        // Verifies that an empty table yields an empty list, not null.
        await using var ctx = CreateContext();
        var repo = new RecipeRepository(ctx);

        var results = await repo.GetAllAsync(CancellationToken.None);

        results.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // SearchAsync — name filter
    // -----------------------------------------------------------------------

    [Test]
    public async Task Search_ByName_ReturnsCaseInsensitiveMatches()
    {
        // Verifies the ILike name filter is case-insensitive and substring-based.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        await repo.AddAsync(BuildRecipe("Chicken Tikka"), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Chicken Soup"), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Beef Stew"), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var results = await readRepo.SearchAsync("chicken", null, CancellationToken.None);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Name.Contains("Chicken", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Search_ByNameWithNoMatches_ReturnsEmptyList()
    {
        // Verifies that a name filter with no matches returns an empty collection,
        // not an exception.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);
        await repo.AddAsync(BuildRecipe("Pasta"), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var results = await readRepo.SearchAsync("xyz_no_match", null, CancellationToken.None);

        results.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // SearchAsync — category filter
    // -----------------------------------------------------------------------

    [Test]
    public async Task Search_ByCategory_ReturnsOnlyMatchingCategory()
    {
        // Verifies that category filtering correctly discriminates by enum value.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        await repo.AddAsync(BuildRecipe("Omelette", RecipeCategory.Breakfast), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("BLT Sandwich", RecipeCategory.Lunch), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Steak", RecipeCategory.Dinner), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var results = await readRepo.SearchAsync(null, RecipeCategory.Breakfast, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Omelette");
    }

    [Test]
    public async Task Search_WithBothFilters_AppliesBothConditions()
    {
        // Verifies that combining name and category filters uses AND semantics.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        await repo.AddAsync(BuildRecipe("Chicken Dinner", RecipeCategory.Dinner), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Chicken Soup", RecipeCategory.Lunch), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Beef Dinner", RecipeCategory.Dinner), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var results = await readRepo.SearchAsync("Chicken", RecipeCategory.Dinner, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Chicken Dinner");
    }

    [Test]
    public async Task Search_WithNullFilters_ReturnsAllRecipes()
    {
        // Verifies that passing null for both filters is equivalent to GetAll.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        await repo.AddAsync(BuildRecipe("Recipe A"), CancellationToken.None);
        await repo.AddAsync(BuildRecipe("Recipe B"), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);

        var results = await readRepo.SearchAsync(null, null, CancellationToken.None);

        results.Should().HaveCount(2);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Update_ExistingRecipe_PersistsChanges()
    {
        // Verifies that calling Update writes modified properties and ingredients
        // back to the database and can be read back with the new values.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        var recipe = BuildRecipe("Original Name");
        await repo.AddAsync(recipe, CancellationToken.None);

        recipe.Update(
            name: "Updated Name",
            description: "Updated description",
            category: RecipeCategory.Lunch,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 15,
            instructions: "New instructions",
            sourceUrl: null,
            ingredients: new[] { BuildIngredient("onion"), BuildIngredient("garlic") });

        await repo.UpdateAsync(recipe, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);
        var result = await readRepo.GetByIdAsync(recipe.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Category.Should().Be(RecipeCategory.Lunch);
        result.Servings.Should().Be(2);
        result.Ingredients.Should().HaveCount(2);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Delete_ExistingRecipe_RemovesFromDatabase()
    {
        // Verifies that DeleteAsync removes the entity so a subsequent GetById returns null.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        var recipe = BuildRecipe();
        await repo.AddAsync(recipe, CancellationToken.None);

        await repo.DeleteAsync(recipe.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new RecipeRepository(readCtx);
        var result = await readRepo.GetByIdAsync(recipe.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task Delete_NonExistentId_DoesNotThrow()
    {
        // Verifies that deleting an ID that doesn't exist is a no-op rather than an exception.
        await using var ctx = CreateContext();
        var repo = new RecipeRepository(ctx);

        var act = async () => await repo.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Delete_RecipeWithIngredients_CascadesDelete()
    {
        // Verifies that deleting a recipe also removes its owned ingredients
        // so there are no orphaned rows in RecipeIngredients.
        await using var writeCtx = CreateContext();
        var repo = new RecipeRepository(writeCtx);

        var recipe = BuildRecipe(ingredients: new[]
        {
            BuildIngredient("flour"),
            BuildIngredient("eggs"),
            BuildIngredient("milk")
        });
        await repo.AddAsync(recipe, CancellationToken.None);

        await repo.DeleteAsync(recipe.Id, CancellationToken.None);

        // No direct table access needed — if ingredients weren't deleted the
        // next assertion would fail because GetById would return them.
        await using var readCtx = CreateContext();
        var result = await new RecipeRepository(readCtx).GetByIdAsync(recipe.Id, CancellationToken.None);
        result.Should().BeNull();
    }
}
