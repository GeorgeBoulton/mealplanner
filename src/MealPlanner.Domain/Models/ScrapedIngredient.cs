using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.Models;

public record ScrapedIngredient(
    string Raw,
    string Name,
    decimal Quantity,
    string Unit,
    ShoppingCategory ShoppingCategory,
    bool Optional
);
