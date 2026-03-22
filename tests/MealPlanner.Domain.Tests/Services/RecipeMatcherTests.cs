using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Services;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Domain.Tests.Services;

[TestFixture]
public class RecipeMatcherTests
{
    private RecipeMatcher _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new RecipeMatcher();
    }

    // A recipe whose every required ingredient is present in the fridge should
    // score exactly 100%, confirming that the match calculation is correct at
    // its upper bound and that the result is included in the output.
    [Test]
    public void Match_AllIngredientsAvailable_Returns100Percent()
    {
        var recipe = Recipe.Create(
            name: "Omelette",
            description: null,
            category: RecipeCategory.Breakfast,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 5,
            instructions: "Whisk and fry.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Egg", 3m, "whole", ShoppingCategory.Dairy),
                new RecipeIngredient("Butter", 10m, "g", ShoppingCategory.Dairy)
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Egg", 6m, "whole"),
            FridgeItem.Create("Butter", 250m, "g")
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().HaveCount(1);
        result[0].MatchPercentage.Should().Be(100m);
        result[0].Recipe.Should().Be(recipe);
    }

    // When none of the required ingredients are in the fridge the score is 0%,
    // which is below the 70% threshold, so the recipe must be excluded entirely
    // rather than returned with a zero score.
    [Test]
    public void Match_NoIngredientsAvailable_ExcludedFromResults()
    {
        var recipe = Recipe.Create(
            name: "Pasta Carbonara",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 10,
            cookTimeMinutes: 15,
            instructions: "Cook pasta.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Pasta", 200m, "g", ShoppingCategory.Dried),
                new RecipeIngredient("Bacon", 100m, "g", ShoppingCategory.Meat)
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Milk", 1m, "litre")
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().BeEmpty();
    }

    // A recipe where only one of three required ingredients is available scores
    // 33%, which is below the 70% threshold, so it must be excluded. This
    // confirms the threshold is applied strictly.
    [Test]
    public void Match_BelowSeventyPercent_ExcludedFromResults()
    {
        var recipe = Recipe.Create(
            name: "Three Ingredient Dish",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 10,
            cookTimeMinutes: 20,
            instructions: "Cook everything.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Chicken", 300m, "g", ShoppingCategory.Meat),
                new RecipeIngredient("Rice", 200m, "g", ShoppingCategory.Dried),
                new RecipeIngredient("Broccoli", 100m, "g", ShoppingCategory.FruitAndVeg)
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Chicken", 400m, "g")  // 1 of 3 = 33%
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().BeEmpty();
    }

    // A recipe where exactly 70% of required ingredients are available sits
    // precisely on the threshold and must be included — the filter is >= 70,
    // not strictly greater.
    [Test]
    public void Match_AboveSeventyPercent_IncludedInResults()
    {
        // 7 ingredients, 7 in fridge = 100%? Use 7 ingredients, 5 available = 71.4%
        // Simpler: 10 ingredients, 7 available = 70% exactly
        var ingredients = Enumerable.Range(1, 10)
            .Select(i => new RecipeIngredient($"Ingredient{i}", 1m, "unit", ShoppingCategory.Other))
            .ToArray();

        var recipe = Recipe.Create(
            name: "Ten Ingredient Recipe",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 4,
            prepTimeMinutes: 15,
            cookTimeMinutes: 30,
            instructions: "Combine all ingredients.",
            sourceUrl: null,
            ingredients: ingredients);

        // Put exactly 7 of the 10 ingredients in the fridge → 70% match
        var fridge = Enumerable.Range(1, 7)
            .Select(i => FridgeItem.Create($"Ingredient{i}", 5m, "unit"))
            .ToArray();

        var result = _sut.Match([recipe], fridge);

        result.Should().HaveCount(1);
        result[0].MatchPercentage.Should().Be(70m);
    }

    // Optional ingredients (e.g. garnishes) are explicitly excluded from the
    // score calculation. This test confirms that a missing optional ingredient
    // does not lower the match percentage, and a present optional ingredient
    // does not inflate it.
    [Test]
    public void Match_OptionalIngredientsIgnored_NotCountedInScore()
    {
        var recipe = Recipe.Create(
            name: "Soup",
            description: null,
            category: RecipeCategory.Lunch,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 30,
            instructions: "Simmer everything.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Tomato", 400m, "g", ShoppingCategory.FruitAndVeg, optional: false),
                new RecipeIngredient("Basil", 5m, "g", ShoppingCategory.FruitAndVeg, optional: true)  // missing but optional
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Tomato", 500m, "g")
            // Basil is NOT in the fridge, but it's optional so score must still be 100%
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().HaveCount(1);
        // Score is based only on the 1 required ingredient (Tomato), which is present
        result[0].MatchPercentage.Should().Be(100m);
    }

    // The name normaliser strips a trailing 's', so a fridge item named "Onions"
    // must match a recipe ingredient named "Onion". This mirrors the same
    // normalisation used in IngredientAggregator.
    [Test]
    public void Match_PluralFridgeItem_MatchesSingularIngredient()
    {
        var recipe = Recipe.Create(
            name: "French Onion Soup",
            description: null,
            category: RecipeCategory.Lunch,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 45,
            instructions: "Caramelise onions.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Onion", 3m, "whole", ShoppingCategory.FruitAndVeg)
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Onions", 5m, "whole")  // plural in fridge, singular in recipe
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().HaveCount(1);
        result[0].MatchPercentage.Should().Be(100m);
    }

    // When multiple recipes are matched, the list must be ordered by
    // MatchPercentage descending so the most actionable recipe (100%) appears
    // before partial matches (e.g. 80%).
    [Test]
    public void Match_ResultsSortedByMatchPercentageDescending()
    {
        // Recipe A: 100% match (both ingredients available)
        var recipeA = Recipe.Create(
            name: "Recipe A",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 10,
            instructions: "Cook A.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Egg", 2m, "whole", ShoppingCategory.Dairy),
                new RecipeIngredient("Butter", 10m, "g", ShoppingCategory.Dairy)
            ]);

        // Recipe B: 80% match (4 of 5 ingredients available)
        var recipeB = Recipe.Create(
            name: "Recipe B",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 10,
            cookTimeMinutes: 20,
            instructions: "Cook B.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Chicken", 300m, "g", ShoppingCategory.Meat),
                new RecipeIngredient("Rice", 200m, "g", ShoppingCategory.Dried),
                new RecipeIngredient("Garlic", 2m, "clove", ShoppingCategory.FruitAndVeg),
                new RecipeIngredient("Onion", 1m, "whole", ShoppingCategory.FruitAndVeg),
                new RecipeIngredient("Soy Sauce", 30m, "ml", ShoppingCategory.Other)  // missing
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Egg", 6m, "whole"),
            FridgeItem.Create("Butter", 250m, "g"),
            FridgeItem.Create("Chicken", 500m, "g"),
            FridgeItem.Create("Rice", 400m, "g"),
            FridgeItem.Create("Garlic", 10m, "clove"),
            FridgeItem.Create("Onion", 3m, "whole")
            // Soy Sauce is missing
        };

        // Supply Recipe B first to verify the sort is not relying on input order
        var result = _sut.Match([recipeB, recipeA], fridge);

        result.Should().HaveCount(2);
        result[0].Recipe.Name.Should().Be("Recipe A");  // 100% first
        result[1].Recipe.Name.Should().Be("Recipe B");  // 80% second
    }

    // A recipe with zero required ingredients (all optional, or the ingredient
    // list is empty) can always be made regardless of what is in the fridge, so
    // it should be treated as a 100% match.
    [Test]
    public void Match_RecipeWithNoRequiredIngredients_Returns100Percent()
    {
        var recipe = Recipe.Create(
            name: "Plain Toast",
            description: null,
            category: RecipeCategory.Breakfast,
            servings: 1,
            prepTimeMinutes: 2,
            cookTimeMinutes: 3,
            instructions: "Toast bread.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Jam", 20m, "g", ShoppingCategory.Other, optional: true)  // only ingredient is optional
            ]);

        var fridge = new[]
        {
            FridgeItem.Create("Milk", 1m, "litre")  // irrelevant — no required ingredients
        };

        var result = _sut.Match([recipe], fridge);

        result.Should().HaveCount(1);
        result[0].MatchPercentage.Should().Be(100m);
    }

    // With an empty fridge there are no ingredients available for any recipe, so
    // every recipe scores 0% and must be excluded (assuming they each have at
    // least one required ingredient). The result list must be empty.
    [Test]
    public void Match_EmptyFridgeItems_ExcludesAllRecipes()
    {
        var recipe = Recipe.Create(
            name: "Scrambled Eggs",
            description: null,
            category: RecipeCategory.Breakfast,
            servings: 1,
            prepTimeMinutes: 2,
            cookTimeMinutes: 5,
            instructions: "Scramble the eggs.",
            sourceUrl: null,
            ingredients:
            [
                new RecipeIngredient("Egg", 3m, "whole", ShoppingCategory.Dairy),
                new RecipeIngredient("Butter", 5m, "g", ShoppingCategory.Dairy)
            ]);

        var result = _sut.Match([recipe], []);

        result.Should().BeEmpty();
    }
}
