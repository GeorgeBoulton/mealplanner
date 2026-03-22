using FluentAssertions;
using MealPlanner.Application.DTOs;
using MealPlanner.Application.Services;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Models;
using MealPlanner.Domain.Services;
using MealPlanner.Domain.ValueObjects;
using NSubstitute;

namespace MealPlanner.Application.Tests;

[TestFixture]
public class RecipeServiceTests
{
    private IRecipeRepository _recipeRepository = null!;
    private IFridgeRepository _fridgeRepository = null!;
    private IRecipeScraper _recipeScraper = null!;
    private RecipeMatcher _recipeMatcher = null!;
    private RecipeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _fridgeRepository = Substitute.For<IFridgeRepository>();
        _recipeScraper = Substitute.For<IRecipeScraper>();
        _recipeMatcher = new RecipeMatcher();
        _sut = new RecipeService(_recipeRepository, _fridgeRepository, _recipeScraper, _recipeMatcher);
    }

    // -- helpers --

    private static Recipe BuildRecipe(string name = "Pasta", RecipeCategory category = RecipeCategory.Dinner,
        IEnumerable<RecipeIngredient>? ingredients = null)
        => Recipe.Create(name, null, category, 2, 10, 20, "Cook it.", null, ingredients);

    private static RecipeIngredient Ingredient(string name, bool optional = false)
        => new(name, 1, "unit", ShoppingCategory.FruitAndVeg, optional);

    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetAllAsync correctly delegates to the repository and maps
    /// all returned recipes to RecipeResponse objects, preserving key fields.
    /// </summary>
    [Test]
    public async Task GetAllAsync_ShouldReturnMappedResponses_WhenRecipesExist()
    {
        var recipe = BuildRecipe("Pasta");
        _recipeRepository.SearchAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<Recipe> { recipe });

        var result = await _sut.GetAllAsync(null, null);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Pasta");
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null when the repository cannot find
    /// a recipe with the given id — the service must not throw.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenRecipeNotFound()
    {
        _recipeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetByIdAsync maps the found recipe to a RecipeResponse
    /// with all fields correctly transferred.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ShouldReturnRecipe_WhenFound()
    {
        var recipe = BuildRecipe("Soup");
        _recipeRepository.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>())
            .Returns(recipe);

        var result = await _sut.GetByIdAsync(recipe.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(recipe.Id);
        result.Name.Should().Be("Soup");
    }

    /// <summary>
    /// Verifies that CreateAsync persists a new Recipe via the repository and
    /// returns a response that reflects the data supplied in the request.
    /// </summary>
    [Test]
    public async Task CreateAsync_ShouldCreateAndReturnRecipe()
    {
        var request = new CreateRecipeRequest(
            "Risotto", null, RecipeCategory.Dinner, 4,
            10, 30, "Stir constantly.", new List<string> { "Italian" },
            new List<RecipeIngredientRequest>
            {
                new("Rice", 200, "g", ShoppingCategory.Dried, false)
            });

        var result = await _sut.CreateAsync(request);

        await _recipeRepository.Received(1).AddAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Risotto");
        result.Category.Should().Be(RecipeCategory.Dinner);
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Name.Should().Be("Rice");
    }

    /// <summary>
    /// Verifies that UpdateAsync returns null when the target recipe does not
    /// exist, without calling UpdateAsync on the repository.
    /// </summary>
    [Test]
    public async Task UpdateAsync_ShouldReturnNull_WhenRecipeNotFound()
    {
        _recipeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Recipe?)null);

        var request = new UpdateRecipeRequest(
            "X", null, RecipeCategory.Lunch, 1, null, null, "Do something.",
            new List<string>(), new List<RecipeIngredientRequest>());

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request);

        result.Should().BeNull();
        await _recipeRepository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that UpdateAsync applies all request fields to the existing
    /// recipe, persists the change, and returns the updated response.
    /// </summary>
    [Test]
    public async Task UpdateAsync_ShouldUpdateAndReturnRecipe_WhenFound()
    {
        var recipe = BuildRecipe("OldName");
        _recipeRepository.GetByIdAsync(recipe.Id, Arg.Any<CancellationToken>())
            .Returns(recipe);

        var request = new UpdateRecipeRequest(
            "NewName", "New desc", RecipeCategory.Lunch, 3, 5, 15, "New instructions.",
            new List<string> { "quick" },
            new List<RecipeIngredientRequest>
            {
                new("Egg", 2, "pcs", ShoppingCategory.Dairy, false)
            });

        var result = await _sut.UpdateAsync(recipe.Id, request);

        await _recipeRepository.Received(1).UpdateAsync(recipe, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Category.Should().Be(RecipeCategory.Lunch);
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Name.Should().Be("Egg");
    }

    /// <summary>
    /// Verifies that DeleteAsync calls the repository with the correct id and
    /// returns true (success is assumed per convention).
    /// </summary>
    [Test]
    public async Task DeleteAsync_ShouldCallRepository()
    {
        var id = Guid.NewGuid();

        var result = await _sut.DeleteAsync(id);

        await _recipeRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ImportAsync orchestrates scraping and recipe creation:
    /// the scraper is called with the given URL, the result is persisted, and
    /// the returned response reflects the scraped data.
    /// </summary>
    [Test]
    public async Task ImportAsync_ShouldScrapeAndCreateRecipe()
    {
        const string url = "https://example.com/recipe";
        var scraped = new ScrapedRecipe(
            "Tacos", "Mexican tacos",
            new List<ScrapedIngredient>
            {
                new("2 tortillas", "Tortilla", 2, "pcs", ShoppingCategory.Dried, false)
            },
            "Assemble and serve.",
            5, 10, 2, "Dinner",
            new List<string> { "mexican" },
            url);

        _recipeScraper.ScrapeAsync(url, Arg.Any<CancellationToken>()).Returns(scraped);

        var result = await _sut.ImportAsync(new ImportRecipeRequest(url));

        await _recipeScraper.Received(1).ScrapeAsync(url, Arg.Any<CancellationToken>());
        await _recipeRepository.Received(1).AddAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Tacos");
        result.SourceUrl.Should().Be(url);
        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Name.Should().Be("Tortilla");
    }

    /// <summary>
    /// Verifies that GetSuggestionsAsync only surfaces recipes at or above the
    /// 70% threshold (RecipeMatcher contract), sorted best-match first, and
    /// that missing ingredients are correctly identified.
    /// </summary>
    [Test]
    public async Task GetSuggestionsAsync_ShouldReturnSortedSuggestions()
    {
        // Recipe A: 2 required ingredients, both in fridge → 100% match
        var recipeA = BuildRecipe("Full Match", ingredients: new[]
        {
            Ingredient("Onion"),
            Ingredient("Garlic")
        });

        // Recipe B: 2 required ingredients, 1 missing → 50% match — below 70%, should be excluded
        var recipeB = BuildRecipe("Half Match", ingredients: new[]
        {
            Ingredient("Onion"),
            Ingredient("Truffle")
        });

        // Recipe C: 1 required ingredient in fridge → 100% match (single ingredient)
        var recipeC = BuildRecipe("Single Ingredient", ingredients: new[]
        {
            Ingredient("Garlic")
        });

        var fridge = new List<FridgeItem>
        {
            FridgeItem.Create("Onions", null, null),
            FridgeItem.Create("Garlic", null, null)
        };

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Recipe> { recipeA, recipeB, recipeC });
        _fridgeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(fridge);

        var result = await _sut.GetSuggestionsAsync();

        // recipeB is below 70%, so only recipeA and recipeC should appear
        result.Should().HaveCount(2);
        result.Should().NotContain(s => s.Recipe.Name == "Half Match");

        // Best match first
        result[0].MatchPercentage.Should().BeGreaterThanOrEqualTo(result[1].MatchPercentage);

        // Full Match has no missing ingredients
        var fullMatch = result.First(s => s.Recipe.Name == "Full Match");
        fullMatch.MissingIngredients.Should().BeEmpty();
    }
}
