using MealPlanner.Application.DTOs;

namespace MealPlanner.Application.Interfaces;

public interface IFridgeService
{
    Task<IReadOnlyList<FridgeItemResponse>> GetAllAsync(CancellationToken ct = default);
    Task<FridgeItemResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FridgeItemResponse> CreateAsync(CreateFridgeItemRequest request, CancellationToken ct = default);
    Task<FridgeItemResponse?> UpdateAsync(Guid id, UpdateFridgeItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default);
}
