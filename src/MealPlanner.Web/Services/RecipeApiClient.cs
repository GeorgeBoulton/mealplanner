using System.Net.Http.Json;
using MealPlanner.Application.DTOs;

namespace MealPlanner.Web.Services;

public class RecipeApiClient(IHttpClientFactory factory)
{
    private HttpClient CreateClient() => factory.CreateClient("MealPlannerApi");

    public async Task<List<RecipeResponse>> GetRecipesAsync(
        string? search = null, string? category = null, string? tag = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrEmpty(tag)) query.Add($"tag={Uri.EscapeDataString(tag)}");
        query.Add($"page={page}");
        query.Add($"pageSize={pageSize}");
        var url = "/api/recipes?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<RecipeResponse>>(url, ct) ?? [];
    }

    public async Task<RecipeResponse?> GetRecipeAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().GetAsync($"/api/recipes/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeResponse>(ct);
    }

    public async Task<RecipeResponse> CreateRecipeAsync(CreateRecipeRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/recipes", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeResponse>(ct))!;
    }

    public async Task<RecipeResponse> UpdateRecipeAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"/api/recipes/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeResponse>(ct))!;
    }

    public async Task DeleteRecipeAsync(Guid id, CancellationToken ct = default)
    {
        var response = await CreateClient().DeleteAsync($"/api/recipes/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<RecipeResponse> ImportRecipeAsync(ImportRecipeRequest request, CancellationToken ct = default)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/recipes/import", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeResponse>(ct))!;
    }

    public async Task<List<RecipeSuggestionResponse>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        return await CreateClient().GetFromJsonAsync<List<RecipeSuggestionResponse>>("/api/recipes/suggestions", ct) ?? [];
    }
}
