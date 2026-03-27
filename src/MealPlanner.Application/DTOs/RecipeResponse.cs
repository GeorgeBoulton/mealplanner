namespace MealPlanner.Application.DTOs;

public record RecipeResponse(Guid Id, string Name, string? Description, RecipeCategory Category, int Servings, int? PrepTimeMinutes, int? CookTimeMinutes, string Instructions, string? SourceUrl, List<string> Tags, List<RecipeIngredientResponse> Ingredients, DateTime CreatedAt, DateTime UpdatedAt);
