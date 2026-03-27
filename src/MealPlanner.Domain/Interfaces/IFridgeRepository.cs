using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Interfaces;

public interface IFridgeRepository
{
    Task<IReadOnlyList<FridgeItem>> GetAllAsync(CancellationToken ct = default);
    Task<FridgeItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(FridgeItem item, CancellationToken ct = default);
    Task UpdateAsync(FridgeItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
}
