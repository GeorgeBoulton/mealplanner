using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Services;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Application.Services;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IFridgeRepository _fridgeRepository;
    private readonly IRecipeScraper _recipeScraper;
    private readonly RecipeMatcher _recipeMatcher;

    public RecipeService(
        IRecipeRepository recipeRepository,
        IFridgeRepository fridgeRepository,
        IRecipeScraper recipeScraper,
        RecipeMatcher recipeMatcher)
    {
        _recipeRepository = recipeRepository;
        _fridgeRepository = fridgeRepository;
        _recipeScraper = recipeScraper;
        _recipeMatcher = recipeMatcher;
    }

    public async Task<IReadOnlyList<RecipeResponse>> GetAllAsync(string? search, RecipeCategory? category, CancellationToken ct = default)
    {
        var recipes = await _recipeRepository.SearchAsync(search, category, ct);
        return recipes.Select(MapToResponse).ToList();
    }

    public async Task<RecipeResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, ct);
        return recipe is null ? null : MapToResponse(recipe);
    }

    public async Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken ct = default)
    {
        var ingredients = MapIngredients(request.Ingredients);
        var recipe = Recipe.Create(
            request.Name,
            request.Description,
            request.Category,
            request.Servings,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Instructions,
            sourceUrl: null,
            ingredients,
            request.Tags);

        await _recipeRepository.AddAsync(recipe, ct);
        return MapToResponse(recipe);
    }

    public async Task<RecipeResponse?> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = await _recipeRepository.GetByIdAsync(id, ct);
        if (recipe is null)
            return null;

        var ingredients = MapIngredients(request.Ingredients);
        recipe.Update(
            request.Name,
            request.Description,
            request.Category,
            request.Servings,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Instructions,
            sourceUrl: recipe.SourceUrl,
            ingredients,
            request.Tags);

        await _recipeRepository.UpdateAsync(recipe, ct);
        return MapToResponse(recipe);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _recipeRepository.DeleteAsync(id, ct);
        return true;
    }

    public async Task<RecipeResponse> ImportAsync(ImportRecipeRequest request, CancellationToken ct = default)
    {
        var scraped = await _recipeScraper.ScrapeAsync(request.Url, ct);

        var ingredients = scraped.Ingredients
            .Select(i => new RecipeIngredient(i.Name, i.Quantity, i.Unit, i.ShoppingCategory, i.Optional))
            .ToList();

        Enum.TryParse<RecipeCategory>(scraped.Category, ignoreCase: true, out var category);
        if (!Enum.IsDefined(typeof(RecipeCategory), category))
            category = RecipeCategory.Dinner;

        var recipe = Recipe.Create(
            scraped.Name,
            scraped.Description,
            category,
            scraped.Servings ?? 1,
            scraped.PrepTimeMinutes,
            scraped.CookTimeMinutes,
            scraped.Instructions ?? string.Empty,
            scraped.SourceUrl,
            ingredients,
            scraped.Tags);

        await _recipeRepository.AddAsync(recipe, ct);
        return MapToResponse(recipe);
    }

    public async Task<IReadOnlyList<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        var recipes = await _recipeRepository.GetAllAsync(ct);
        var fridgeItems = await _fridgeRepository.GetAllAsync(ct);

        var fridgeNames = fridgeItems
            .Select(f => NormaliseName(f.Name))
            .ToHashSet();

        var matches = _recipeMatcher.Match(recipes, fridgeItems);

        return matches
            .Select(m =>
            {
                var missing = m.Recipe.Ingredients
                    .Where(i => !i.Optional && !fridgeNames.Contains(NormaliseName(i.Name)))
                    .Select(i => i.Name)
                    .ToList();

                return new RecipeSuggestionResponse(MapToResponse(m.Recipe), m.MatchPercentage, missing);
            })
            .ToList();
    }

    // Mirrors the normalisation in RecipeMatcher so missing-ingredient names are consistent
    private static string NormaliseName(string name)
    {
        var normalised = name.Trim().ToLowerInvariant();
        if (normalised.EndsWith('s') && normalised.Length > 1)
            normalised = normalised[..^1];
        return normalised;
    }

    private static List<RecipeIngredient> MapIngredients(List<RecipeIngredientRequest> requests)
        => requests
            .Select(r => new RecipeIngredient(r.Name, r.Quantity, r.Unit, r.ShoppingCategory, r.Optional))
            .ToList();

    private static RecipeResponse MapToResponse(Recipe recipe)
        => new(
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
            recipe.Ingredients
                .Select(i => new RecipeIngredientResponse(i.Name, i.Quantity, i.Unit, i.ShoppingCategory, i.Optional))
                .ToList(),
            recipe.CreatedAt,
            recipe.UpdatedAt);
}
