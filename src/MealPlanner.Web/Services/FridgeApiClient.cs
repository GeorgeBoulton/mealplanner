using System.Net.Http.Json;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Web.Services;

public class FridgeApiClient(IHttpClientFactory factory)
{
    private HttpClient CreateClient() => factory.CreateClient("MealPlannerApi");

    public async Task<List<FridgeItemResponse>> GetFridgeItemsAsync(CancellationToken ct = default)
    {
        return await CreateClient().GetFromJsonAsync<List<FridgeItemResponse>>("/api/fridge", ct) ?? [];
    }

    public async Task<FridgeItemResponse> CreateFridgeItemAsync(CreateFridgeItemRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/fridge", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FridgeItemResponse>(ct))!;
    }

    public async Task<FridgeItemResponse> UpdateFridgeItemAsync(Guid id, UpdateFridgeItemRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"/api/fridge/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FridgeItemResponse>(ct))!;
    }

    public async Task DeleteFridgeItemAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().DeleteAsync($"/api/fridge/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<List<RecipeSuggestionResponse>>("/api/fridge/suggestions", ct) ?? [];
    }
}
