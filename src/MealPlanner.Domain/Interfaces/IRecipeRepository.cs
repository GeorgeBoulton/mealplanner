using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> SearchAsync(string? nameFilter, RecipeCategory? category, CancellationToken ct = default);
    Task AddAsync(Recipe recipe, CancellationToken ct = default);
    Task UpdateAsync(Recipe recipe, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
