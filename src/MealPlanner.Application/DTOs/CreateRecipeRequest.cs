using MealPlanner.Domain.Enums;

namespace MealPlanner.Application.DTOs;

public record CreateRecipeRequest(string Name, string? Description, RecipeCategory Category, int Servings, int? PrepTimeMinutes, int? CookTimeMinutes, string Instructions, List<string> Tags, List<RecipeIngredientRequest> Ingredients);
