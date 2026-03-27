using FluentAssertions;
using MealPlanner.Application.DTOs;
using MealPlanner.Application.Services;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Services;
using MealPlanner.Domain.ValueObjects;
using NSubstitute;
using DomainEnums = MealPlanner.Domain.Enums;

namespace MealPlanner.Application.Tests;

[TestFixture]
public class FridgeServiceTests
{
    private IFridgeRepository _fridgeRepository = null!;
    private IRecipeRepository _recipeRepository = null!;
    private RecipeMatcher _recipeMatcher = null!;
    private FridgeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fridgeRepository = Substitute.For<IFridgeRepository>();
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _recipeMatcher = new RecipeMatcher();
        _sut = new FridgeService(_fridgeRepository, _recipeRepository, _recipeMatcher);
    }

    /// <summary>
    /// Ensures the service returns an empty list rather than null when the
    /// repository contains no fridge items, so callers never need to null-check.
    /// </summary>
    [Test]
    public async Task GetAllAsync_WhenNoItems_ReturnsEmptyList()
    {
        // Ensures an empty repository returns an empty list (not null)
        _fridgeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FridgeItem>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
        await _fridgeRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures all fridge items returned by the repository are correctly mapped
    /// to FridgeItemResponse DTOs, preserving name, quantity, and unit for each entry.
    /// </summary>
    [Test]
    public async Task GetAllAsync_WithItems_ReturnsMappedResponses()
    {
        // Ensures all fridge items are mapped to response DTOs correctly
        var item1 = FridgeItem.Create("Milk", 2m, "litres");
        var item2 = FridgeItem.Create("Eggs", 12m, null);
        _fridgeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FridgeItem> { item1, item2 });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Milk");
        result[0].Quantity.Should().Be(2m);
        result[0].Unit.Should().Be("litres");
        result[1].Name.Should().Be("Eggs");
        result[1].Quantity.Should().Be(12m);
        result[1].Unit.Should().BeNull();
    }

    /// <summary>
    /// Ensures the service returns null instead of throwing when the requested
    /// fridge item does not exist, protecting callers from unhandled exceptions.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_WhenItemNotFound_ReturnsNull()
    {
        // Ensures null is returned rather than throwing when item does not exist
        _fridgeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((FridgeItem?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// Ensures a found fridge item is correctly mapped to a FridgeItemResponse
    /// with all fields (id, name, quantity, unit) accurately transferred.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_WhenItemFound_ReturnsMappedResponse()
    {
        // Ensures a found item is correctly mapped to FridgeItemResponse
        var item = FridgeItem.Create("Butter", 250m, "g");
        _fridgeRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(item);

        var result = await _sut.GetByIdAsync(item.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(item.Id);
        result.Name.Should().Be("Butter");
        result.Quantity.Should().Be(250m);
        result.Unit.Should().Be("g");
    }

    /// <summary>
    /// Ensures CreateAsync builds a FridgeItem from the request, persists it via
    /// the repository's AddAsync, and returns a response that reflects the supplied data.
    /// </summary>
    [Test]
    public async Task CreateAsync_CreatesItemWithCorrectDataAndCallsAddAsync()
    {
        // Ensures CreateAsync persists the new item and returns accurate response
        var request = new CreateFridgeItemRequest("Cheese", 300m, "g");

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Cheese");
        result.Quantity.Should().Be(300m);
        result.Unit.Should().Be("g");
        result.Id.Should().NotBe(Guid.Empty);
        await _fridgeRepository.Received(1).AddAsync(
            Arg.Is<FridgeItem>(f => f.Name == "Cheese" && f.Quantity == 300m && f.Unit == "g"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures UpdateAsync returns null instead of throwing when the target item
    /// does not exist, and that the repository's UpdateAsync is never called in that case.
    /// </summary>
    [Test]
    public async Task UpdateAsync_WhenItemNotFound_ReturnsNull()
    {
        // Ensures UpdateAsync returns null without throwing when item does not exist
        _fridgeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((FridgeItem?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateFridgeItemRequest("X", null, null));

        result.Should().BeNull();
        await _fridgeRepository.DidNotReceive().UpdateAsync(Arg.Any<FridgeItem>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures UpdateAsync applies all changed fields to the existing item, delegates
    /// persistence to the repository's UpdateAsync, and returns the updated response.
    /// </summary>
    [Test]
    public async Task UpdateAsync_WhenItemFound_UpdatesItemAndCallsRepositoryUpdateAsync()
    {
        // Ensures UpdateAsync mutates the item and persists changes via the repository
        var item = FridgeItem.Create("Milk", 1m, "litre");
        _fridgeRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(item);

        var result = await _sut.UpdateAsync(item.Id, new UpdateFridgeItemRequest("Skimmed Milk", 2m, "litres"));

        result.Should().NotBeNull();
        result!.Id.Should().Be(item.Id);
        result.Name.Should().Be("Skimmed Milk");
        result.Quantity.Should().Be(2m);
        result.Unit.Should().Be("litres");
        await _fridgeRepository.Received(1).UpdateAsync(
            Arg.Is<FridgeItem>(f => f.Name == "Skimmed Milk" && f.Quantity == 2m),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures DeleteAsync delegates to the repository with the correct id and
    /// returns true to signal successful deletion to the caller.
    /// </summary>
    [Test]
    public async Task DeleteAsync_CallsRepositoryDeleteAsyncAndReturnsTrue()
    {
        // Ensures DeleteAsync delegates to the repository and signals success
        var id = Guid.NewGuid();

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        await _fridgeRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures ClearAllAsync delegates to the repository's ClearAllAsync so that
    /// all fridge items are removed in a single operation.
    /// </summary>
    [Test]
    public async Task ClearAllAsync_CallsRepositoryClearAllAsync()
    {
        // Ensures ClearAllAsync delegates to the repository
        await _sut.ClearAllAsync();

        await _fridgeRepository.Received(1).ClearAllAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ensures GetSuggestionsAsync wires the real RecipeMatcher with live fridge
    /// and recipe data, returning suggestions with a non-zero match percentage when
    /// the fridge contains ingredients required by a recipe.
    /// </summary>
    [Test]
    public async Task GetSuggestionsAsync_ReturnsSuggestionsFromRecipeMatcher()
    {
        // Ensures suggestions are computed from the real RecipeMatcher using fridge and recipe data
        var ingredient = new RecipeIngredient("Eggs", 3m, string.Empty, DomainEnums.ShoppingCategory.Dairy, false);
        var recipe = Recipe.Create(
            "Scrambled Eggs",
            "Simple scrambled eggs",
            DomainEnums.RecipeCategory.Breakfast,
            2,
            5,
            10,
            "Whisk and cook eggs.",
            null,
            new List<RecipeIngredient> { ingredient },
            new List<string> { "quick" });

        var fridgeItem = FridgeItem.Create("Eggs", 6m, null);

        _recipeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Recipe> { recipe });
        _fridgeRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<FridgeItem> { fridgeItem });

        var result = await _sut.GetSuggestionsAsync();

        result.Should().NotBeEmpty();
        result[0].Recipe.Name.Should().Be("Scrambled Eggs");
        result[0].MatchPercentage.Should().BeGreaterThan(0);
    }
}
