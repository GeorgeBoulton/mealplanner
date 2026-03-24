using System.Net.Http.Json;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Web.Services;

public class MealPlanApiClient(IHttpClientFactory factory)
{
    private HttpClient CreateClient() => factory.CreateClient("MealPlannerApi");

    public async Task<List<MealPlanResponse>> GetMealPlansAsync(CancellationToken ct = default)
    {
        return await CreateClient().GetFromJsonAsync<List<MealPlanResponse>>("/api/mealplans", ct) ?? [];
    }

    public async Task<MealPlanResponse?> GetMealPlanAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().GetAsync($"/api/mealplans/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct);
    }

    public async Task<MealPlanResponse?> GetCurrentMealPlanAsync(CancellationToken ct = default)
    {
        var response = await CreateClient().GetAsync("/api/mealplans/current", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct);
    }

    public async Task<MealPlanResponse> CreateMealPlanAsync(CreateMealPlanRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/mealplans", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct))!;
    }

    public async Task DeleteMealPlanAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().DeleteAsync($"/api/mealplans/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<MealPlanResponse> AddEntryAsync(Guid id, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync($"/api/mealplans/{id}/entries", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct))!;
    }

    public async Task<MealPlanResponse> UpdateEntryAsync(Guid id, Guid entryId, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"/api/mealplans/{id}/entries/{entryId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct))!;
    }

    public async Task<MealPlanResponse> DeleteEntryAsync(Guid id, Guid entryId, CancellationToken ct = default)
    {
        var response = await CreateClient().DeleteAsync($"/api/mealplans/{id}/entries/{entryId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealPlanResponse>(ct))!;
    }
}
