namespace MealPlanner.Domain.Interfaces;

public class ScrapedRecipe
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Instructions { get; init; } = "";
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public int? Servings { get; init; }
    public List<ScrapedIngredient> Ingredients { get; init; } = new();
}

public class ScrapedIngredient
{
    public string Raw { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = "";
}
