namespace MealPlanner.Application.DTOs;

public record CreateFridgeItemRequest(string Name, decimal? Quantity, string? Unit);
