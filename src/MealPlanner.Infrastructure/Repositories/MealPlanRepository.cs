using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Repositories;

public class MealPlanRepository : IMealPlanRepository
{
    private readonly MealPlannerDbContext _context;

    public MealPlanRepository(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MealPlans
            .Include(m => m.Entries)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<MealPlan?> GetByWeekAsync(DateOnly weekStartDate, CancellationToken ct = default)
    {
        return await _context.MealPlans
            .Include(m => m.Entries)
            .FirstOrDefaultAsync(m => m.WeekStartDate == weekStartDate, ct);
    }

    public async Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.MealPlans
            .Include(m => m.Entries)
            .ToListAsync(ct);
    }

    public async Task AddAsync(MealPlan mealPlan, CancellationToken ct = default)
    {
        await _context.MealPlans.AddAsync(mealPlan, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MealPlan mealPlan, CancellationToken ct = default)
    {
        foreach (var entry in mealPlan.Entries)
        {
            if (_context.Entry(entry).State == EntityState.Detached)
                _context.MealPlanEntries.Add(entry);
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var mealPlan = await _context.MealPlans.FindAsync(new object[] { id }, ct);
        if (mealPlan is null)
            return;

        _context.MealPlans.Remove(mealPlan);
        await _context.SaveChangesAsync(ct);
    }
}
