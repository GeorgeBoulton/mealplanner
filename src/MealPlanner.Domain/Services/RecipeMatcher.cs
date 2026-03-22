using MealPlanner.Domain.Entities;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Domain.Services;

public class RecipeMatcher
{
    /// <summary>
    /// Scores each recipe by how many of its required (non-optional) ingredients
    /// are available in the fridge, then returns only those recipes that score
    /// at or above the 70% threshold, sorted best-match first.
    /// </summary>
    public IReadOnlyList<RecipeMatch> Match(
        IEnumerable<Recipe> recipes,
        IEnumerable<FridgeItem> fridgeItems)
    {
        // Build a set of normalised fridge-item names for O(1) lookup
        var fridgeNames = fridgeItems
            .Select(f => NormaliseName(f.Name))
            .ToHashSet();

        var results = new List<RecipeMatch>();

        foreach (var recipe in recipes)
        {
            var requiredIngredients = recipe.Ingredients
                .Where(i => !i.Optional)
                .ToList();

            decimal matchPercentage;

            if (requiredIngredients.Count == 0)
            {
                // Edge case: no required ingredients — the recipe can always be made
                matchPercentage = 100m;
            }
            else
            {
                int available = requiredIngredients
                    .Count(i => fridgeNames.Contains(NormaliseName(i.Name)));

                matchPercentage = (decimal)available / requiredIngredients.Count * 100m;
            }

            // Recipes below 70% are not worth surfacing to the user
            if (matchPercentage >= 70m)
                results.Add(new RecipeMatch(recipe, matchPercentage));
        }

        // Best matches first so the user sees the most actionable recipes at the top
        return results
            .OrderByDescending(r => r.MatchPercentage)
            .ToList();
    }

    /// <summary>
    /// Normalises an ingredient or fridge-item name for comparison:
    /// lowercase, trim whitespace, and strip a trailing 's' so that
    /// "Onions" in the fridge matches "Onion" in a recipe.
    /// Mirrors the logic in <see cref="IngredientAggregator"/>.
    /// </summary>
    private static string NormaliseName(string name)
    {
        var normalised = name.Trim().ToLowerInvariant();
        if (normalised.EndsWith('s') && normalised.Length > 1)
            normalised = normalised[..^1];
        return normalised;
    }
}
