namespace MealPlanner.Application.DTOs;

public record RecipeIngredientRequest(string Name, decimal Quantity, string Unit, ShoppingCategory ShoppingCategory, bool Optional);
