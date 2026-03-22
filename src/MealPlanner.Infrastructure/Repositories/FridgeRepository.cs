using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Repositories;

public class FridgeRepository : IFridgeRepository
{
    private readonly MealPlannerDbContext _context;

    public FridgeRepository(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FridgeItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.FridgeItems.ToListAsync(ct);
    }

    public async Task<FridgeItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.FridgeItems.FindAsync(new object[] { id }, ct);
    }

    public async Task AddAsync(FridgeItem item, CancellationToken ct = default)
    {
        await _context.FridgeItems.AddAsync(item, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FridgeItem item, CancellationToken ct = default)
    {
        _context.Update(item);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var fridgeItem = await _context.FridgeItems.FindAsync(new object[] { id }, ct);
        if (fridgeItem is null)
            return;

        _context.FridgeItems.Remove(fridgeItem);
        await _context.SaveChangesAsync(ct);
    }
}
