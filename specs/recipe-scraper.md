# Recipe scraper specification

## Overview
Import recipes from URLs by scraping the page and extracting structured
recipe data. Many recipe sites use JSON-LD structured data (Schema.org
Recipe type), which is the primary extraction method.

## Implementation

### Primary: JSON-LD extraction
Most major recipe sites embed structured data in a `<script type="application/ld+json">` tag using the Schema.org Recipe schema.

Extract:
- `name` → Recipe.Name
- `description` → Recipe.Description
- `recipeIngredient` (array of strings) → parse into RecipeIngredient objects
- `recipeInstructions` → Recipe.Instructions (join steps into text)
- `prepTime` (ISO 8601 duration) → Recipe.PrepTimeMinutes
- `cookTime` (ISO 8601 duration) → Recipe.CookTimeMinutes
- `recipeYield` → Recipe.Servings (parse the number)
- `recipeCategory` → map to RecipeCategory enum (best effort)
- `keywords` → Recipe.Tags

### Ingredient parsing
The `recipeIngredient` field is typically an array of strings like:
- "500g beef mince"
- "2 tablespoons olive oil"
- "1 large onion, diced"
- "Salt and pepper to taste"

Parse each string into:
- Quantity (decimal): extract the leading number
- Unit (string): extract common units (g, kg, ml, l, tbsp, tsp, cup, etc)
- Name (string): the rest of the string after quantity and unit
- ShoppingCategory: best-effort classification based on ingredient name
- Optional: mark as optional if it contains words like "optional", "garnish", "to taste", "to serve"

This parsing does not need to be perfect — the user can edit the recipe
after import. Aim for 80% accuracy on common recipe sites.

### Fallback: basic HTML scraping
If no JSON-LD is found, attempt basic extraction from common HTML patterns
(look for elements with class names like "recipe-ingredients", "instructions", etc).
This is best-effort — if it fails, return an error saying the URL couldn't be parsed
and suggest manual entry.

### Supported sites (test against these)
- BBC Good Food (bbcgoodfood.com)
- Allrecipes (allrecipes.com)
- Delicious Magazine (deliciousmagazine.co.uk)
- Jamie Oliver (jamieoliver.com)

### Interface
```csharp
// In Domain
public interface IRecipeScraper
{
    Task<ScrapedRecipe> ScrapeAsync(string url, CancellationToken ct);
}

public record ScrapedRecipe(
    string Name,
    string? Description,
    List<ScrapedIngredient> Ingredients,
    string? Instructions,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int? Servings,
    string? Category,
    List<string> Tags,
    string SourceUrl
);
```

### Dependencies
- Use `HtmlAgilityPack` for HTML parsing
- Use `System.Text.Json` for JSON-LD extraction
- HttpClient for fetching pages (register as named client with a sensible User-Agent)
