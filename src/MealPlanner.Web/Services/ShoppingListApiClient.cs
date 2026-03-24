using System.Net.Http.Json;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Web.Services;

public class ShoppingListApiClient(IHttpClientFactory factory)
{
    private HttpClient CreateClient() => factory.CreateClient("MealPlannerApi");

    public async Task<ShoppingListResponse> GenerateFromMealPlanAsync(Guid mealPlanId, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync($"/api/mealplans/{mealPlanId}/shopping-list", (object?)null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShoppingListResponse>(ct))!;
    }

    public async Task<ShoppingListResponse?> GetShoppingListAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().GetAsync($"/api/shopping-lists/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShoppingListResponse>(ct);
    }

    public async Task<string> ExportShoppingListAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().GetAsync($"/api/shopping-lists/{id}/export", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<ShoppingListItemResponse> UpdateItemAsync(Guid id, Guid itemId, UpdateShoppingListItemRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"/api/shopping-lists/{id}/items/{itemId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShoppingListItemResponse>(ct))!;
    }

    public async Task DeleteShoppingListAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().DeleteAsync($"/api/shopping-lists/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
