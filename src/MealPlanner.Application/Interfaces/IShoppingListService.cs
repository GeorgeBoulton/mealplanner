using MealPlanner.Application.DTOs;

namespace MealPlanner.Application.Interfaces;

public interface IShoppingListService
{
    Task<ShoppingListResponse?> GenerateAsync(Guid mealPlanId, CancellationToken ct = default);
    Task<ShoppingListResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ShoppingListResponse?> GetByMealPlanAsync(Guid mealPlanId, CancellationToken ct = default);
    Task<ShoppingListResponse?> UpdateItemAsync(Guid shoppingListId, Guid itemId, UpdateShoppingListItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string?> ExportAsync(Guid id, CancellationToken ct = default);
}
