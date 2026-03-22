namespace MealPlanner.Application.DTOs;

public record MealPlanResponse(Guid Id, DateOnly WeekStartDate, List<MealPlanEntryResponse> Entries, DateTime CreatedAt);
