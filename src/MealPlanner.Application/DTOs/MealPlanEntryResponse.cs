namespace MealPlanner.Application.DTOs;

public record MealPlanEntryResponse(Guid Id, DateOnly Date, MealType MealType, Guid RecipeId, int Servings);
