namespace MealPlanner.Application.DTOs;

public record ShoppingListItemResponse(Guid Id, string IngredientName, decimal TotalQuantity, string Unit, ShoppingCategory Category, bool IsChecked, List<string> FromRecipes);
