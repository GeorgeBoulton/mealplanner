namespace MealPlanner.Application.DTOs;

public record ShoppingListResponse(Guid Id, Guid MealPlanId, List<ShoppingListItemResponse> Items, DateTime GeneratedAt);
