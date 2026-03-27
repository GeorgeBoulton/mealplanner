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
public class ShoppingListServiceTests
{
    private IShoppingListRepository _shoppingListRepository = null!;
    private IMealPlanRepository _mealPlanRepository = null!;
    private IRecipeRepository _recipeRepository = null!;
    private IngredientAggregator _ingredientAggregator = null!;
    private ShoppingListService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _shoppingListRepository = Substitute.For<IShoppingListRepository>();
        _mealPlanRepository = Substitute.For<IMealPlanRepository>();
        _recipeRepository = Substitute.For<IRecipeRepository>();
        _ingredientAggregator = new IngredientAggregator();
        _sut = new ShoppingListService(
            _shoppingListRepository,
            _mealPlanRepository,
            _recipeRepository,
            _ingredientAggregator);
    }

    // -- helpers --

    private static MealPlan BuildMealPlan(DateOnly? weekStart = null)
    {
        var date = weekStart ?? new DateOnly(2025, 3, 17);
        return MealPlan.Create(date);
    }

    private static Recipe BuildRecipe(
        string name = "Pasta",
        int servings = 2,
        IEnumerable<RecipeIngredient>? ingredients = null)
        => Recipe.Create(name, null, DomainEnums.RecipeCategory.Dinner, servings, 10, 20, "Cook.", null, ingredients);

    private static RecipeIngredient Ingredient(
        string name,
        decimal quantity = 1,
        string unit = "kg",
        DomainEnums.ShoppingCategory category = DomainEnums.ShoppingCategory.FruitAndVeg,
        bool optional = false)
        => new(name, quantity, unit, category, optional);

    private static ShoppingList BuildShoppingList(Guid mealPlanId)
    {
        var item = ShoppingListItem.Create(Guid.Empty, "Onion", 2, "kg", DomainEnums.ShoppingCategory.FruitAndVeg);
        return ShoppingList.Create(mealPlanId, new[] { item });
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// GenerateAsync must return null when no meal plan exists for the given ID,
    /// because there is nothing to generate a list from.
    /// </summary>
    [Test]
    public async Task GenerateAsync_WhenMealPlanNotFound_ReturnsNull()
    {
        _mealPlanRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.GenerateAsync(Guid.NewGuid());

        result.Should().BeNull();
        await _shoppingListRepository.DidNotReceive().AddAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// GenerateAsync must aggregate ingredients from all meal plan entries and
    /// persist a new ShoppingList. The returned DTO must reflect the aggregated items.
    /// </summary>
    [Test]
    public async Task GenerateAsync_WithMealPlanEntries_CreatesShoppingListWithAggregatedIngredients()
    {
        var mealPlan = BuildMealPlan();

        var recipe1 = BuildRecipe("Pasta", servings: 2, ingredients: new[]
        {
            Ingredient("Onion", 1, "kg", DomainEnums.ShoppingCategory.FruitAndVeg)
        });
        var recipe2 = BuildRecipe("Soup", servings: 4, ingredients: new[]
        {
            Ingredient("Carrot", 2, "kg", DomainEnums.ShoppingCategory.FruitAndVeg)
        });

        var entry1 = MealPlanEntry.Create(mealPlan.Id, mealPlan.WeekStartDate, DomainEnums.MealType.Dinner, recipe1.Id, 2);
        var entry2 = MealPlanEntry.Create(mealPlan.Id, mealPlan.WeekStartDate.AddDays(1), DomainEnums.MealType.Lunch, recipe2.Id, 4);
        mealPlan.AddEntry(entry1);
        mealPlan.AddEntry(entry2);

        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);
        _recipeRepository.GetByIdAsync(recipe1.Id, Arg.Any<CancellationToken>())
            .Returns(recipe1);
        _recipeRepository.GetByIdAsync(recipe2.Id, Arg.Any<CancellationToken>())
            .Returns(recipe2);
        _shoppingListRepository.GetByMealPlanAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns((ShoppingList?)null);

        var result = await _sut.GenerateAsync(mealPlan.Id);

        await _shoppingListRepository.Received(1).AddAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.MealPlanId.Should().Be(mealPlan.Id);
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.IngredientName == "Onion");
        result.Items.Should().Contain(i => i.IngredientName == "Carrot");
    }

    /// <summary>
    /// GenerateAsync must delete any pre-existing shopping list for the same meal plan
    /// before creating the new one, to avoid duplicate lists.
    /// </summary>
    [Test]
    public async Task GenerateAsync_DeletesExistingShoppingListBeforeCreatingNew()
    {
        var mealPlan = BuildMealPlan();
        var existingList = BuildShoppingList(mealPlan.Id);

        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);
        _shoppingListRepository.GetByMealPlanAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(existingList);

        var result = await _sut.GenerateAsync(mealPlan.Id);

        await _shoppingListRepository.Received(1).DeleteAsync(existingList.Id, Arg.Any<CancellationToken>());
        await _shoppingListRepository.Received(1).AddAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
    }

    /// <summary>
    /// GetByIdAsync must return null when no shopping list exists with the given ID,
    /// so the caller can distinguish between found and not-found.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _shoppingListRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingList?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// GetByIdAsync must map the domain entity to a ShoppingListResponse,
    /// correctly transferring all fields including items.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var mealPlanId = Guid.NewGuid();
        var shoppingList = BuildShoppingList(mealPlanId);

        _shoppingListRepository.GetByIdAsync(shoppingList.Id, Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        var result = await _sut.GetByIdAsync(shoppingList.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(shoppingList.Id);
        result.MealPlanId.Should().Be(mealPlanId);
        result.Items.Should().HaveCount(1);
        result.Items[0].IngredientName.Should().Be("Onion");
    }

    /// <summary>
    /// UpdateItemAsync must return null when the shopping list does not exist,
    /// so the API layer can respond with 404 Not Found.
    /// </summary>
    [Test]
    public async Task UpdateItemAsync_WhenShoppingListNotFound_ReturnsNull()
    {
        _shoppingListRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingList?)null);

        var result = await _sut.UpdateItemAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateShoppingListItemRequest(IsChecked: true));

        result.Should().BeNull();
        await _shoppingListRepository.DidNotReceive().UpdateAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// UpdateItemAsync must return null when the item ID does not exist within
    /// the shopping list, so the API layer can respond with 404 Not Found.
    /// </summary>
    [Test]
    public async Task UpdateItemAsync_WhenItemNotFound_ReturnsNull()
    {
        var mealPlanId = Guid.NewGuid();
        var shoppingList = BuildShoppingList(mealPlanId);

        _shoppingListRepository.GetByIdAsync(shoppingList.Id, Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        var result = await _sut.UpdateItemAsync(
            shoppingList.Id,
            Guid.NewGuid(), // non-existent item ID
            new UpdateShoppingListItemRequest(IsChecked: true));

        result.Should().BeNull();
        await _shoppingListRepository.DidNotReceive().UpdateAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// UpdateItemAsync must toggle the item's IsChecked state when the requested
    /// state differs from the current one, then persist and return the updated list.
    /// </summary>
    [Test]
    public async Task UpdateItemAsync_TogglesCheckedAndSaves()
    {
        var mealPlanId = Guid.NewGuid();
        var shoppingList = BuildShoppingList(mealPlanId);
        var item = shoppingList.Items[0]; // IsChecked = false by default

        _shoppingListRepository.GetByIdAsync(shoppingList.Id, Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        // Request IsChecked = true — different from current false → should toggle
        var result = await _sut.UpdateItemAsync(
            shoppingList.Id,
            item.Id,
            new UpdateShoppingListItemRequest(IsChecked: true));

        await _shoppingListRepository.Received(1).UpdateAsync(shoppingList, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Items[0].IsChecked.Should().BeTrue();
    }

    /// <summary>
    /// DeleteAsync must delegate to the repository with the correct ID.
    /// </summary>
    [Test]
    public async Task DeleteAsync_CallsRepository()
    {
        var id = Guid.NewGuid();

        var result = await _sut.DeleteAsync(id);

        await _shoppingListRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        result.Should().BeTrue();
    }

    /// <summary>
    /// ExportAsync must return null when the shopping list is not found,
    /// so the caller knows there is nothing to export.
    /// </summary>
    [Test]
    public async Task ExportAsync_WhenNotFound_ReturnsNull()
    {
        _shoppingListRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ShoppingList?)null);

        var result = await _sut.ExportAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// ExportAsync must produce a plain-text representation grouped by category,
    /// with category emoji headers and correctly formatted item lines.
    /// </summary>
    [Test]
    public async Task ExportAsync_FormatsGroupedByCategory()
    {
        var mealPlanId = Guid.NewGuid();

        var vegItem  = ShoppingListItem.Create(Guid.Empty, "Onion",  2,   "kg", DomainEnums.ShoppingCategory.FruitAndVeg);
        var meatItem = ShoppingListItem.Create(Guid.Empty, "Chicken", 0.5m, "kg", DomainEnums.ShoppingCategory.Meat);
        var dairyItem = ShoppingListItem.Create(Guid.Empty, "Milk", 1, "l", DomainEnums.ShoppingCategory.Dairy);

        var shoppingList = ShoppingList.Create(mealPlanId, new[] { vegItem, meatItem, dairyItem });

        _shoppingListRepository.GetByIdAsync(shoppingList.Id, Arg.Any<CancellationToken>())
            .Returns(shoppingList);

        var result = await _sut.ExportAsync(shoppingList.Id);

        result.Should().NotBeNull();

        // Category headers should appear
        result.Should().Contain("Fruit & Veg");
        result.Should().Contain("Meat");
        result.Should().Contain("Dairy");

        // Item lines with quantity and unit
        result.Should().Contain("- 2 kg Onion");
        result.Should().Contain("- 0.5 kg Chicken");
        result.Should().Contain("- 1 l Milk");

        // FruitAndVeg (enum value 0) should appear before Meat (1) and Dairy (3)
        var vegIndex   = result!.IndexOf("Fruit & Veg", StringComparison.Ordinal);
        var meatIndex  = result.IndexOf("Meat",         StringComparison.Ordinal);
        var dairyIndex = result.IndexOf("Dairy",        StringComparison.Ordinal);
        vegIndex.Should().BeLessThan(meatIndex);
        meatIndex.Should().BeLessThan(dairyIndex);
    }
}
