namespace MealPlanner.Domain.Models;

public record ScrapedRecipe(
    string Name,
    string? Description,
    List<ScrapedIngredient> Ingredients,
    string? Instructions,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int? Servings,
    string? Category,
    List<string> Tags,
    string SourceUrl
);
