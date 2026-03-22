using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Repositories;

public class ShoppingListRepository : IShoppingListRepository
{
    private readonly MealPlannerDbContext _context;

    public ShoppingListRepository(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<ShoppingList?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ShoppingLists
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<ShoppingList?> GetByMealPlanAsync(Guid mealPlanId, CancellationToken ct = default)
    {
        return await _context.ShoppingLists
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.MealPlanId == mealPlanId, ct);
    }

    public async Task AddAsync(ShoppingList shoppingList, CancellationToken ct = default)
    {
        await _context.ShoppingLists.AddAsync(shoppingList, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ShoppingList shoppingList, CancellationToken ct = default)
    {
        _context.Update(shoppingList);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var shoppingList = await _context.ShoppingLists.FindAsync(new object[] { id }, ct);
        if (shoppingList is null)
            return;

        _context.ShoppingLists.Remove(shoppingList);
        await _context.SaveChangesAsync(ct);
    }
}
