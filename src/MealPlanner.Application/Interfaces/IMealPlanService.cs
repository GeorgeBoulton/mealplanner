using MealPlanner.Application.DTOs;

namespace MealPlanner.Application.Interfaces;

public interface IMealPlanService
{
    Task<IReadOnlyList<MealPlanResponse>> GetAllAsync(CancellationToken ct = default);
    Task<MealPlanResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MealPlanResponse> GetOrCreateCurrentWeekAsync(CancellationToken ct = default);
    Task<MealPlanResponse> CreateAsync(CreateMealPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MealPlanResponse?> AddEntryAsync(Guid id, MealPlanEntryRequest request, CancellationToken ct = default);
    Task<MealPlanResponse?> UpdateEntryAsync(Guid id, Guid entryId, MealPlanEntryRequest request, CancellationToken ct = default);
    Task<MealPlanResponse?> RemoveEntryAsync(Guid id, Guid entryId, CancellationToken ct = default);
}
