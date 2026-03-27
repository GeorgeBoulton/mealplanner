using MealPlanner.Application.DTOs;

namespace MealPlanner.Application.Interfaces;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeResponse>> GetAllAsync(string? search, RecipeCategory? category, CancellationToken ct = default);
    Task<RecipeResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeResponse?> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<RecipeResponse> ImportAsync(ImportRecipeRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default);
}
