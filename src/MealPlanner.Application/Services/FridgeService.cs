using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Services;

namespace MealPlanner.Application.Services;

public class FridgeService : IFridgeService
{
    private readonly IFridgeRepository _fridgeRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly RecipeMatcher _recipeMatcher;

    public FridgeService(
        IFridgeRepository fridgeRepository,
        IRecipeRepository recipeRepository,
        RecipeMatcher recipeMatcher)
    {
        _fridgeRepository = fridgeRepository;
        _recipeRepository = recipeRepository;
        _recipeMatcher = recipeMatcher;
    }

    public async Task<IReadOnlyList<FridgeItemResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _fridgeRepository.GetAllAsync(ct);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<FridgeItemResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _fridgeRepository.GetByIdAsync(id, ct);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<FridgeItemResponse> CreateAsync(CreateFridgeItemRequest request, CancellationToken ct = default)
    {
        var item = FridgeItem.Create(request.Name, request.Quantity, request.Unit);
        await _fridgeRepository.AddAsync(item, ct);
        return MapToResponse(item);
    }

    public async Task<FridgeItemResponse?> UpdateAsync(Guid id, UpdateFridgeItemRequest request, CancellationToken ct = default)
    {
        var item = await _fridgeRepository.GetByIdAsync(id, ct);
        if (item is null)
            return null;

        item.Update(request.Name, request.Quantity, request.Unit);
        await _fridgeRepository.UpdateAsync(item, ct);
        return MapToResponse(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _fridgeRepository.DeleteAsync(id, ct);
        return true;
    }

    public async Task<IReadOnlyList<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        var recipes = await _recipeRepository.GetAllAsync(ct);
        var fridgeItems = await _fridgeRepository.GetAllAsync(ct);

        var matches = _recipeMatcher.Match(recipes, fridgeItems);

        return matches.Select(m =>
        {
            var missingIngredients = m.Recipe.Ingredients
                .Where(i => !i.Optional)
                .Where(i => !fridgeItems.Any(f =>
                    NormaliseName(f.Name) == NormaliseName(i.Name)))
                .Select(i => i.Name)
                .ToList();

            return new RecipeSuggestionResponse(
                MapToRecipeResponse(m.Recipe),
                m.MatchPercentage,
                missingIngredients);
        }).ToList();
    }

    private static FridgeItemResponse MapToResponse(FridgeItem item) =>
        new(item.Id, item.Name, item.Quantity, item.Unit, item.AddedAt);

    private static RecipeResponse MapToRecipeResponse(Recipe recipe) =>
        new(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Category,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.Instructions,
            recipe.SourceUrl,
            recipe.Tags.ToList(),
            recipe.Ingredients.Select(i => new RecipeIngredientResponse(
                i.Name,
                i.Quantity,
                i.Unit,
                i.ShoppingCategory,
                i.Optional)).ToList(),
            recipe.CreatedAt,
            recipe.UpdatedAt);

    private static string NormaliseName(string name) =>
        name.Trim().ToLowerInvariant().TrimEnd('s');
}
