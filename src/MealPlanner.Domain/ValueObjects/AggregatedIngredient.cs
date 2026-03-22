using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.ValueObjects;

public record AggregatedIngredient(
    string Name,
    decimal TotalQuantity,
    string Unit,
    ShoppingCategory Category,
    IReadOnlyList<string> FromRecipes);
