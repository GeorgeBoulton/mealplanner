using MealPlanner.Domain.Enums;

namespace MealPlanner.Application.DTOs;

public record RecipeIngredientResponse(string Name, decimal Quantity, string Unit, ShoppingCategory ShoppingCategory, bool Optional);
