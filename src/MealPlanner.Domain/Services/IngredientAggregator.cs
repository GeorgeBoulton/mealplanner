using MealPlanner.Domain.Entities;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Domain.Services;

public class IngredientAggregator
{
    /// <summary>
    /// Aggregates ingredients across all meal plan entries, scaling each recipe's
    /// ingredient quantities by the ratio of planned servings to recipe servings,
    /// then combining ingredients that share the same normalised name and unit.
    /// </summary>
    public IReadOnlyList<AggregatedIngredient> Aggregate(
        IEnumerable<(MealPlanEntry Entry, Recipe Recipe)> meals)
    {
        // Key: (normalised name, unit) → accumulated data
        var groups = new Dictionary<(string NormalisedName, string Unit), GroupAccumulator>();

        foreach (var (entry, recipe) in meals)
        {
            decimal scale = recipe.Servings > 0
                ? (decimal)entry.Servings / recipe.Servings
                : 1m;

            foreach (var ingredient in recipe.Ingredients)
            {
                // Optional ingredients (e.g. garnishes) are excluded from the shopping list
                if (ingredient.Optional)
                    continue;

                string normalisedName = NormaliseName(ingredient.Name);
                var key = (normalisedName, ingredient.Unit);

                if (!groups.TryGetValue(key, out var acc))
                {
                    // First time we see this ingredient — use its display name as-is
                    acc = new GroupAccumulator(ingredient.Name, ingredient.ShoppingCategory);
                    groups[key] = acc;
                }

                acc.TotalQuantity += ingredient.Quantity * scale;

                // Track each recipe that contributes to this ingredient group
                if (!acc.FromRecipes.Contains(recipe.Name))
                    acc.FromRecipes.Add(recipe.Name);
            }
        }

        return groups
            .Select(kvp => new AggregatedIngredient(
                Name: kvp.Value.DisplayName,
                TotalQuantity: kvp.Value.TotalQuantity,
                Unit: kvp.Key.Unit,
                Category: kvp.Value.Category,
                FromRecipes: kvp.Value.FromRecipes.AsReadOnly()))
            .ToList();
    }

    /// <summary>
    /// Normalises an ingredient name for grouping purposes:
    /// lowercase, trim whitespace, and strip a trailing 's' so that
    /// "Onion" and "onions" map to the same group key.
    /// </summary>
    private static string NormaliseName(string name)
    {
        var normalised = name.Trim().ToLowerInvariant();
        if (normalised.EndsWith('s') && normalised.Length > 1)
            normalised = normalised[..^1];
        return normalised;
    }

    private sealed class GroupAccumulator(string displayName, global::MealPlanner.Domain.Enums.ShoppingCategory category)
    {
        public string DisplayName { get; } = displayName;
        public global::MealPlanner.Domain.Enums.ShoppingCategory Category { get; } = category;
        public decimal TotalQuantity { get; set; }
        public List<string> FromRecipes { get; } = new();
    }
}
