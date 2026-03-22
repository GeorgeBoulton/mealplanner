using System.Text.RegularExpressions;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Models;

namespace MealPlanner.Infrastructure.Scrapers;

internal static class IngredientParser
{
    private static readonly string[] OptionalKeywords =
        ["optional", "garnish", "to taste", "to serve", "if desired"];

    // Common units in priority order (longer strings first to avoid partial matches)
    private static readonly string[] Units =
    [
        "tablespoons", "tablespoon", "teaspoons", "teaspoon",
        "tbsp", "tsp", "cups", "cup",
        "ounces", "ounce", "oz",
        "pounds", "pound", "lb", "lbs",
        "litres", "litre", "liter", "liters", "l",
        "millilitres", "millilitre", "milliliter", "milliliters", "ml",
        "kilograms", "kilogram", "kg",
        "grams", "gram", "g",
        "pinch", "handful", "bunch", "cloves", "clove",
        "slices", "slice", "sheets", "sheet",
        "cans", "can", "jars", "jar",
    ];

    public static ScrapedIngredient Parse(string raw)
    {
        var text = raw.Trim();
        var lower = text.ToLowerInvariant();

        var optional = OptionalKeywords.Any(k => lower.Contains(k));

        // Try to extract quantity
        decimal quantity = 0;
        var remaining = text;

        var quantityMatch = Regex.Match(text,
            @"^(\d+(?:[\/\.\,]\d+)?(?:\s*-\s*\d+(?:[\/\.\,]\d+)?)?)\s*");

        if (quantityMatch.Success)
        {
            quantity = ParseQuantity(quantityMatch.Groups[1].Value);
            remaining = text[quantityMatch.Length..].Trim();
        }

        // Try to extract unit
        var unit = "";
        foreach (var candidate in Units)
        {
            if (remaining.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
            {
                var after = remaining[candidate.Length..];
                if (after.Length == 0 || !char.IsLetter(after[0]))
                {
                    unit = candidate;
                    remaining = after.TrimStart('.', ' ', ',');
                    break;
                }
            }
        }

        // Strip parenthetical notes
        remaining = Regex.Replace(remaining, @"\([^)]*\)", "").Trim();
        // Strip trailing comma-separated descriptors like ", diced" → keep everything before comma for short names
        var name = remaining.TrimEnd(',', '.', ' ');

        var category = ClassifyCategory(name.ToLowerInvariant());

        return new ScrapedIngredient(raw, name, quantity, unit, category, optional);
    }

    private static decimal ParseQuantity(string raw)
    {
        // Handle ranges like "1-2": take the first number
        var first = raw.Split('-')[0].Trim();
        // Handle fractions like "1/2"
        if (first.Contains('/'))
        {
            var parts = first.Split('/');
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out var num) &&
                decimal.TryParse(parts[1].Trim(), out var den) &&
                den != 0)
                return Math.Round(num / den, 3);
        }

        first = first.Replace(',', '.');
        return decimal.TryParse(first, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static ShoppingCategory ClassifyCategory(string name)
    {
        if (ContainsAny(name, ["beef", "chicken", "pork", "lamb", "mince", "steak",
                "turkey", "veal", "bacon", "sausage", "ham", "salami"]))
            return ShoppingCategory.Meat;

        if (ContainsAny(name, ["salmon", "tuna", "cod", "haddock", "prawn", "shrimp",
                "fish", "anchovy", "mackerel", "sardine", "crab", "lobster"]))
            return ShoppingCategory.Fish;

        if (ContainsAny(name, ["milk", "cream", "butter", "cheese", "yogurt",
                "yoghurt", "egg", "creme fraiche", "sour cream", "double cream"]))
            return ShoppingCategory.Dairy;

        if (ContainsAny(name, ["bread", "flour", "roll", "baguette", "tortilla",
                "pita", "naan", "croissant", "bagel"]))
            return ShoppingCategory.Bakery;

        if (ContainsAny(name, ["tomato", "onion", "garlic", "potato", "carrot",
                "pepper", "courgette", "zucchini", "broccoli", "spinach", "kale",
                "lettuce", "cucumber", "mushroom", "apple", "banana", "lemon",
                "lime", "orange", "berries", "strawberry", "raspberry", "avocado",
                "celery", "leek", "spring onion", "chilli", "chile", "herb",
                "parsley", "basil", "thyme", "rosemary", "coriander", "mint"]))
            return ShoppingCategory.FruitAndVeg;

        if (ContainsAny(name, ["pasta", "rice", "lentil", "chickpea", "bean",
                "oat", "quinoa", "couscous", "noodle", "breadcrumb", "dried"]))
            return ShoppingCategory.Dried;

        if (ContainsAny(name, ["tin", "canned", "can of", "chopped tomato",
                "coconut milk", "stock cube", "broth", "bouillon"]))
            return ShoppingCategory.Tinned;

        if (ContainsAny(name, ["oil", "vinegar", "sauce", "soy", "mustard",
                "ketchup", "mayonnaise", "honey", "syrup", "jam", "paste",
                "salt", "pepper", "spice", "cumin", "paprika", "turmeric",
                "cinnamon", "sugar", "vanilla", "baking", "yeast"]))
            return ShoppingCategory.Condiments;

        if (ContainsAny(name, ["frozen", "ice cream", "peas frozen"]))
            return ShoppingCategory.Frozen;

        if (ContainsAny(name, ["juice", "wine", "beer", "water", "soda",
                "sparkling", "tea", "coffee", "stock"]))
            return ShoppingCategory.Drinks;

        return ShoppingCategory.Other;
    }

    private static bool ContainsAny(string text, IEnumerable<string> keywords)
        => keywords.Any(text.Contains);
}
