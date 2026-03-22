namespace MealPlanner.Application.DTOs;

public record FridgeItemResponse(Guid Id, string Name, decimal? Quantity, string? Unit, DateTime AddedAt);
