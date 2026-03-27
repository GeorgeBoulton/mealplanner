namespace MealPlanner.Application.DTOs;

public record MealPlanEntryRequest(DateOnly Date, MealType MealType, Guid RecipeId, int Servings);
