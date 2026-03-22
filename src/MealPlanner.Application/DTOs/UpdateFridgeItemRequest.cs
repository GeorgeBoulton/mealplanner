namespace MealPlanner.Application.DTOs;

public record UpdateFridgeItemRequest(string Name, decimal? Quantity, string? Unit);
