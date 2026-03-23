using System.Text;
using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Services;

namespace MealPlanner.Application.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IngredientAggregator _ingredientAggregator;

    public ShoppingListService(
        IShoppingListRepository shoppingListRepository,
        IMealPlanRepository mealPlanRepository,
        IRecipeRepository recipeRepository,
        IngredientAggregator ingredientAggregator)
    {
        _shoppingListRepository = shoppingListRepository;
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
        _ingredientAggregator = ingredientAggregator;
    }

    public async Task<ShoppingListResponse?> GenerateAsync(Guid mealPlanId, CancellationToken ct = default)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(mealPlanId, ct);
        if (mealPlan is null)
            return null;

        // Fetch each recipe referenced by the meal plan entries
        var meals = new List<(MealPlanEntry Entry, Recipe Recipe)>();
        foreach (var entry in mealPlan.Entries)
        {
            var recipe = await _recipeRepository.GetByIdAsync(entry.RecipeId, ct);
            if (recipe is not null)
                meals.Add((entry, recipe));
        }

        // Aggregate ingredients across all entries, scaling by servings
        var aggregated = _ingredientAggregator.Aggregate(meals);

        // Delete any existing shopping list for this meal plan before creating a new one
        var existing = await _shoppingListRepository.GetByMealPlanAsync(mealPlanId, ct);
        if (existing is not null)
            await _shoppingListRepository.DeleteAsync(existing.Id, ct);

        // Create items — pass Guid.Empty as shoppingListId; EF Core will resolve
        // the foreign key correctly via the navigation property when SaveChanges is called
        var items = aggregated
            .Select(a => ShoppingListItem.Create(
                Guid.Empty,
                a.Name,
                a.TotalQuantity,
                a.Unit,
                a.Category,
                a.FromRecipes))
            .ToList();

        var shoppingList = ShoppingList.Create(mealPlanId, items);
        await _shoppingListRepository.AddAsync(shoppingList, ct);

        return MapToResponse(shoppingList);
    }

    public async Task<ShoppingListResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var shoppingList = await _shoppingListRepository.GetByIdAsync(id, ct);
        return shoppingList is null ? null : MapToResponse(shoppingList);
    }

    public async Task<ShoppingListResponse?> GetByMealPlanAsync(Guid mealPlanId, CancellationToken ct = default)
    {
        var shoppingList = await _shoppingListRepository.GetByMealPlanAsync(mealPlanId, ct);
        return shoppingList is null ? null : MapToResponse(shoppingList);
    }

    public async Task<ShoppingListResponse?> UpdateItemAsync(
        Guid shoppingListId,
        Guid itemId,
        UpdateShoppingListItemRequest request,
        CancellationToken ct = default)
    {
        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, ct);
        if (shoppingList is null)
            return null;

        var item = shoppingList.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return null;

        if (request.IsChecked != item.IsChecked)
            item.ToggleChecked();

        await _shoppingListRepository.UpdateAsync(shoppingList, ct);
        return MapToResponse(shoppingList);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _shoppingListRepository.DeleteAsync(id, ct);
        return true;
    }

    public async Task<string?> ExportAsync(Guid id, CancellationToken ct = default)
    {
        var shoppingList = await _shoppingListRepository.GetByIdAsync(id, ct);
        if (shoppingList is null)
            return null;

        var sb = new StringBuilder();

        var grouped = shoppingList.Items
            .GroupBy(i => i.Category)
            .OrderBy(g => (int)g.Key);

        foreach (var group in grouped)
        {
            sb.AppendLine(GetCategoryHeader(group.Key));

            foreach (var item in group)
            {
                if (item.TotalQuantity > 0 && !string.IsNullOrEmpty(item.Unit))
                {
                    var quantity = FormatQuantity(item.TotalQuantity);
                    sb.AppendLine($"- {quantity} {item.Unit} {item.IngredientName}");
                }
                else
                {
                    sb.AppendLine($"- {item.IngredientName}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatQuantity(decimal quantity)
    {
        // Show as integer if it is a whole number, otherwise up to 2 decimal places
        return quantity == Math.Floor(quantity)
            ? ((int)quantity).ToString()
            : quantity.ToString("0.##");
    }

    private static string GetCategoryHeader(ShoppingCategory category) => category switch
    {
        ShoppingCategory.FruitAndVeg  => "\U0001f966 Fruit & Veg",
        ShoppingCategory.Meat         => "\U0001f969 Meat",
        ShoppingCategory.Fish         => "\U0001f41f Fish",
        ShoppingCategory.Dairy        => "\U0001f95b Dairy",
        ShoppingCategory.Bakery       => "\U0001f35e Bakery",
        ShoppingCategory.Tinned       => "\U0001f96b Tinned",
        ShoppingCategory.Dried        => "\U0001f33e Dried",
        ShoppingCategory.Frozen       => "\U0001f9ca Frozen",
        ShoppingCategory.Condiments   => "\U0001fad9 Condiments",
        ShoppingCategory.Drinks       => "\U0001f964 Drinks",
        ShoppingCategory.Snacks       => "\U0001f37f Snacks",
        ShoppingCategory.Household    => "\U0001f9f9 Household",
        ShoppingCategory.Other        => "\U0001f4e6 Other",
        _                             => category.ToString()
    };

    private static ShoppingListResponse MapToResponse(ShoppingList shoppingList)
        => new(
            shoppingList.Id,
            shoppingList.MealPlanId,
            shoppingList.Items
                .Select(i => new ShoppingListItemResponse(
                    i.Id,
                    i.IngredientName,
                    i.TotalQuantity,
                    i.Unit,
                    i.Category,
                    i.IsChecked,
                    i.FromRecipes.ToList()))
                .ToList(),
            shoppingList.GeneratedAt);
}
