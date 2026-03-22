using MealPlanner.Domain.Enums;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Domain.Entities;

public class Recipe
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public RecipeCategory Category { get; private set; }
    public int Servings { get; private set; }
    public int? PrepTimeMinutes { get; private set; }
    public int? CookTimeMinutes { get; private set; }
    public string Instructions { get; private set; }
    public string? SourceUrl { get; private set; }

    private readonly List<RecipeIngredient> _ingredients = new();
    public IReadOnlyList<RecipeIngredient> Ingredients => _ingredients;

    private readonly List<string> _tags = new();
    public IReadOnlyList<string> Tags => _tags;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Recipe()
    {
        Name = string.Empty;
        Instructions = string.Empty;
    }

    public static Recipe Create(
        string name,
        string? description,
        RecipeCategory category,
        int servings,
        int? prepTimeMinutes,
        int? cookTimeMinutes,
        string instructions,
        string? sourceUrl,
        IEnumerable<RecipeIngredient>? ingredients = null,
        IEnumerable<string>? tags = null)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            Servings = servings,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            Instructions = instructions,
            SourceUrl = sourceUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (ingredients != null)
            recipe._ingredients.AddRange(ingredients);

        if (tags != null)
            recipe._tags.AddRange(tags);

        return recipe;
    }

    public void Update(
        string name,
        string? description,
        RecipeCategory category,
        int servings,
        int? prepTimeMinutes,
        int? cookTimeMinutes,
        string instructions,
        string? sourceUrl,
        IEnumerable<RecipeIngredient>? ingredients = null,
        IEnumerable<string>? tags = null)
    {
        Name = name;
        Description = description;
        Category = category;
        Servings = servings;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Instructions = instructions;
        SourceUrl = sourceUrl;
        UpdatedAt = DateTime.UtcNow;

        if (ingredients != null)
        {
            _ingredients.Clear();
            _ingredients.AddRange(ingredients);
        }

        if (tags != null)
        {
            _tags.Clear();
            _tags.AddRange(tags);
        }
    }
}
