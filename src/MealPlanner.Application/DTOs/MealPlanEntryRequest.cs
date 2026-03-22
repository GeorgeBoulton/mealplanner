using MealPlanner.Domain.Enums;

namespace MealPlanner.Application.DTOs;

public record MealPlanEntryRequest(DateOnly Date, MealType MealType, Guid RecipeId, int Servings);
