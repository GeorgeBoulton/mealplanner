using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Interfaces;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MealPlan?> GetByWeekAsync(DateOnly weekStartDate, CancellationToken ct = default);
    Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(MealPlan mealPlan, CancellationToken ct = default);
    Task UpdateAsync(MealPlan mealPlan, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
