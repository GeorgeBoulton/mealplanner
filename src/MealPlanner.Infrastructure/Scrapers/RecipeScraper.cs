using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Exceptions;
using MealPlanner.Domain.Interfaces;
using MealPlanner.Domain.Models;

namespace MealPlanner.Infrastructure.Scrapers;

public class RecipeScraper : IRecipeScraper
{
    private readonly HttpClient _httpClient;

    public RecipeScraper(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("RecipeScraper");
    }

    public async Task<ScrapedRecipe> ScrapeAsync(string url, CancellationToken ct = default)
    {
        string html;
        try
        {
            html = await _httpClient.GetStringAsync(url, ct);
        }
        catch (TaskCanceledException)
        {
            throw new RecipeScrapingException($"Request timed out for URL: {url}");
        }
        catch (HttpRequestException ex)
        {
            throw new RecipeScrapingException($"Failed to fetch the URL: {url}. {ex.Message}");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var jsonLd = TryExtractJsonLd(doc);
        if (jsonLd is not null)
        {
            return ParseJsonLd(jsonLd, url);
        }

        return TryFallbackScrape(doc, url)
            ?? throw new RecipeScrapingException(
                $"Could not parse recipe from '{url}'. The page does not contain recognisable recipe data. Please enter the recipe manually.");
    }

    // ── JSON-LD ──────────────────────────────────────────────────────────────

    private static JsonDocument? TryExtractJsonLd(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode
            .SelectNodes("//script[@type='application/ld+json']");

        if (scripts is null) return null;

        foreach (var script in scripts)
        {
            try
            {
                var json = JsonDocument.Parse(script.InnerText);
                if (IsRecipeSchema(json.RootElement))
                    return json;

                // Check @graph array
                if (json.RootElement.TryGetProperty("@graph", out var graph) &&
                    graph.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in graph.EnumerateArray())
                    {
                        if (IsRecipeSchema(item))
                        {
                            // Wrap in new document for consistent handling
                            var innerJson = item.GetRawText();
                            return JsonDocument.Parse(innerJson);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // skip malformed blocks
            }
        }

        return null;
    }

    private static bool IsRecipeSchema(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object) return false;
        if (!el.TryGetProperty("@type", out var type)) return false;

        return type.ValueKind == JsonValueKind.String
            ? type.GetString()?.Contains("Recipe", StringComparison.OrdinalIgnoreCase) == true
            : type.ValueKind == JsonValueKind.Array &&
              type.EnumerateArray().Any(t =>
                  t.GetString()?.Contains("Recipe", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static ScrapedRecipe ParseJsonLd(JsonDocument json, string url)
    {
        var root = json.RootElement;

        var name = GetString(root, "name") ?? "Untitled Recipe";
        var description = GetString(root, "description");
        var instructions = ParseInstructions(root);
        var prepTime = ParseIsoDuration(GetString(root, "prepTime"));
        var cookTime = ParseIsoDuration(GetString(root, "cookTime"));
        var servings = ParseServings(root);
        var category = GetString(root, "recipeCategory");
        var tags = ParseTags(root);
        var ingredients = ParseIngredients(root);

        return new ScrapedRecipe(
            name, description, ingredients, instructions,
            prepTime, cookTime, servings, category, tags, url);
    }

    private static string? ParseInstructions(JsonElement root)
    {
        if (!root.TryGetProperty("recipeInstructions", out var inst))
            return null;

        if (inst.ValueKind == JsonValueKind.String)
            return inst.GetString();

        if (inst.ValueKind == JsonValueKind.Array)
        {
            var steps = new List<string>();
            var i = 1;
            foreach (var step in inst.EnumerateArray())
            {
                string? text = null;
                if (step.ValueKind == JsonValueKind.String)
                    text = step.GetString();
                else if (step.ValueKind == JsonValueKind.Object)
                    text = GetString(step, "text") ?? GetString(step, "name");

                if (!string.IsNullOrWhiteSpace(text))
                    steps.Add($"{i++}. {text.Trim()}");
            }
            return steps.Count > 0 ? string.Join("\n", steps) : null;
        }

        return null;
    }

    private static int? ParseServings(JsonElement root)
    {
        if (!root.TryGetProperty("recipeYield", out var yield))
            return null;

        var raw = yield.ValueKind == JsonValueKind.String
            ? yield.GetString()
            : yield.ValueKind == JsonValueKind.Array
                ? yield.EnumerateArray().FirstOrDefault().GetString()
                : null;

        if (raw is null) return null;

        var match = Regex.Match(raw, @"\d+");
        return match.Success ? int.Parse(match.Value) : null;
    }

    private static List<string> ParseTags(JsonElement root)
    {
        var tags = new List<string>();

        if (root.TryGetProperty("keywords", out var kw))
        {
            if (kw.ValueKind == JsonValueKind.String)
            {
                tags.AddRange(kw.GetString()!
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0));
            }
            else if (kw.ValueKind == JsonValueKind.Array)
            {
                tags.AddRange(kw.EnumerateArray()
                    .Select(t => t.GetString()?.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => t!));
            }
        }

        return tags;
    }

    private static List<ScrapedIngredient> ParseIngredients(JsonElement root)
    {
        if (!root.TryGetProperty("recipeIngredient", out var arr) ||
            arr.ValueKind != JsonValueKind.Array)
            return [];

        return arr.EnumerateArray()
            .Select(el => el.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => IngredientParser.Parse(s!))
            .ToList();
    }

    // ── Fallback HTML scraping ───────────────────────────────────────────────

    private static ScrapedRecipe? TryFallbackScrape(HtmlDocument doc, string url)
    {
        // Try to find ingredient lists by common class name patterns
        var ingredientNodes = doc.DocumentNode.SelectNodes(
            "//*[contains(@class,'ingredient')]//li | //*[contains(@class,'ingredients')]//li");

        if (ingredientNodes is null || ingredientNodes.Count == 0)
            return null;

        var ingredients = ingredientNodes
            .Select(n => n.InnerText.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => IngredientParser.Parse(t))
            .ToList();

        if (ingredients.Count == 0)
            return null;

        var title = doc.DocumentNode
            .SelectSingleNode("//h1")?.InnerText.Trim() ?? "Untitled Recipe";

        return new ScrapedRecipe(
            title, null, ingredients, null,
            null, null, null, null, [], url);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetString(JsonElement el, string property)
    {
        return el.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private static int? ParseIsoDuration(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;

        // PT1H30M → 90 minutes
        var match = Regex.Match(iso, @"PT(?:(\d+)H)?(?:(\d+)M)?", RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        var minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        var total = hours * 60 + minutes;
        return total > 0 ? total : null;
    }
}
