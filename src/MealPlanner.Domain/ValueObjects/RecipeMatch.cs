using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.ValueObjects;

public record RecipeMatch(Recipe Recipe, decimal MatchPercentage);
