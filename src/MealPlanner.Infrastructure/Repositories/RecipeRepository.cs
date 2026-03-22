using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly MealPlannerDbContext _context;

    public RecipeRepository(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Recipes
            .Include(r => r.Ingredients)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Recipe>> SearchAsync(
        string? nameFilter,
        RecipeCategory? category,
        CancellationToken ct = default)
    {
        var query = _context.Recipes
            .Include(r => r.Ingredients)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(r => EF.Functions.ILike(r.Name, $"%{nameFilter}%"));
        }

        if (category.HasValue)
        {
            query = query.Where(r => r.Category == category.Value);
        }

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken ct = default)
    {
        await _context.Recipes.AddAsync(recipe, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken ct = default)
    {
        _context.Update(recipe);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await _context.Recipes.FindAsync(new object[] { id }, ct);
        if (recipe is null)
            return;

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync(ct);
    }
}
