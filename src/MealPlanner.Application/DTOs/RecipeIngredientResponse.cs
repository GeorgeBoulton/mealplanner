namespace MealPlanner.Application.DTOs;

public record RecipeIngredientResponse(string Name, decimal Quantity, string Unit, ShoppingCategory ShoppingCategory, bool Optional);
