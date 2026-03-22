namespace MealPlanner.Application.DTOs;

public record RecipeSuggestionResponse(RecipeResponse Recipe, decimal MatchPercentage, List<string> MissingIngredients);
