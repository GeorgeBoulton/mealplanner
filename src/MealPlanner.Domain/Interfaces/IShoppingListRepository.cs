using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Interfaces;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ShoppingList?> GetByMealPlanAsync(Guid mealPlanId, CancellationToken ct = default);
    Task AddAsync(ShoppingList shoppingList, CancellationToken ct = default);
    Task UpdateAsync(ShoppingList shoppingList, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
