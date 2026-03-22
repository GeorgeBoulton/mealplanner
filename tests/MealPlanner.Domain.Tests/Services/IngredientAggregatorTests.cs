using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Services;
using MealPlanner.Domain.ValueObjects;

namespace MealPlanner.Domain.Tests.Services;

[TestFixture]
public class IngredientAggregatorTests
{
    private IngredientAggregator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new IngredientAggregator();
    }

    // Verifies the happy-path baseline: a single recipe with a single ingredient
    // passes straight through aggregation unchanged.
    [Test]
    public void Aggregate_SingleRecipe_SingleIngredient_ReturnsIngredient()
    {
        var ingredient = new RecipeIngredient("Chicken", 500m, "g", ShoppingCategory.Meat);
        var recipe = Recipe.Create(
            name: "Roast Chicken",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 60,
            instructions: "Roast it.",
            sourceUrl: null,
            ingredients: [ingredient]);

        var entry = MealPlanEntry.Create(
            mealPlanId: Guid.NewGuid(),
            date: new DateOnly(2026, 3, 22),
            mealType: MealType.Dinner,
            recipeId: recipe.Id,
            servings: 4);

        var result = _sut.Aggregate([(entry, recipe)]);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Chicken");
        result[0].TotalQuantity.Should().Be(500m);
        result[0].Unit.Should().Be("g");
        result[0].Category.Should().Be(ShoppingCategory.Meat);
    }

    // Verifies that when a meal plan entry requests fewer (or more) servings than
    // the recipe's default serving count, each ingredient quantity is scaled by
    // entry.Servings / recipe.Servings.
    [Test]
    public void Aggregate_SingleRecipe_ScalesQuantityByServings()
    {
        // Recipe is for 4 servings; entry requests 2 → scale factor = 0.5
        var ingredient = new RecipeIngredient("Rice", 400m, "g", ShoppingCategory.Dried);
        var recipe = Recipe.Create(
            name: "Rice Bowl",
            description: null,
            category: RecipeCategory.Lunch,
            servings: 4,
            prepTimeMinutes: 5,
            cookTimeMinutes: 20,
            instructions: "Cook rice.",
            sourceUrl: null,
            ingredients: [ingredient]);

        var entry = MealPlanEntry.Create(
            mealPlanId: Guid.NewGuid(),
            date: new DateOnly(2026, 3, 22),
            mealType: MealType.Lunch,
            recipeId: recipe.Id,
            servings: 2);

        var result = _sut.Aggregate([(entry, recipe)]);

        result.Should().HaveCount(1);
        result[0].TotalQuantity.Should().Be(200m); // 400 * (2/4)
    }

    // Verifies that when two recipes share the same ingredient (same name and unit),
    // they are merged into a single shopping list line with summed quantities.
    [Test]
    public void Aggregate_TwoRecipes_SameIngredientSameUnit_CombinesQuantity()
    {
        var mealPlanId = Guid.NewGuid();

        var recipe1 = Recipe.Create(
            name: "Omelette",
            description: null,
            category: RecipeCategory.Breakfast,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 5,
            instructions: "Whisk eggs.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Egg", 3m, "whole", ShoppingCategory.Dairy)]);

        var recipe2 = Recipe.Create(
            name: "Fried Rice",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 10,
            cookTimeMinutes: 15,
            instructions: "Fry rice.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Egg", 2m, "whole", ShoppingCategory.Dairy)]);

        var entry1 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Breakfast, recipe1.Id, 2);
        var entry2 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Dinner, recipe2.Id, 2);

        var result = _sut.Aggregate([(entry1, recipe1), (entry2, recipe2)]);

        result.Should().HaveCount(1);
        result[0].TotalQuantity.Should().Be(5m); // 3 + 2, both at 1× scale
    }

    // Verifies that the normalisation logic treats "onions" (plural) and "onion"
    // (singular) as the same ingredient, so they are aggregated rather than kept
    // as two separate shopping list lines.
    [Test]
    public void Aggregate_PluralName_MatchesSingular()
    {
        var mealPlanId = Guid.NewGuid();

        var recipe1 = Recipe.Create(
            name: "Soup",
            description: null,
            category: RecipeCategory.Lunch,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 30,
            instructions: "Make soup.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Onion", 1m, "whole", ShoppingCategory.FruitAndVeg)]);

        var recipe2 = Recipe.Create(
            name: "Stew",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 4,
            prepTimeMinutes: 15,
            cookTimeMinutes: 60,
            instructions: "Make stew.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Onions", 2m, "whole", ShoppingCategory.FruitAndVeg)]);

        var entry1 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Lunch, recipe1.Id, 4);
        var entry2 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Dinner, recipe2.Id, 4);

        var result = _sut.Aggregate([(entry1, recipe1), (entry2, recipe2)]);

        // The two onion entries should merge into one line totalling 3
        result.Should().HaveCount(1);
        result[0].TotalQuantity.Should().Be(3m);
    }

    // Verifies that ingredients flagged as optional (e.g. a garnish or seasoning to
    // taste) are silently excluded from the aggregated shopping list.
    [Test]
    public void Aggregate_SkipsOptionalIngredients()
    {
        var required = new RecipeIngredient("Pasta", 200m, "g", ShoppingCategory.Dried, optional: false);
        var optional = new RecipeIngredient("Parsley", 5m, "g", ShoppingCategory.FruitAndVeg, optional: true);

        var recipe = Recipe.Create(
            name: "Pasta Dish",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 10,
            instructions: "Cook pasta.",
            sourceUrl: null,
            ingredients: [required, optional]);

        var entry = MealPlanEntry.Create(Guid.NewGuid(), new DateOnly(2026, 3, 22), MealType.Dinner, recipe.Id, 2);

        var result = _sut.Aggregate([(entry, recipe)]);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Pasta");
    }

    // Verifies that the same ingredient name used with two different units is kept as
    // two separate lines, because "500g flour" and "2 tbsp flour" cannot be meaningfully
    // combined into a single quantity.
    [Test]
    public void Aggregate_SameIngredientDifferentUnit_KeepsSeparate()
    {
        var mealPlanId = Guid.NewGuid();

        var recipe1 = Recipe.Create(
            name: "Bread",
            description: null,
            category: RecipeCategory.Snack,
            servings: 1,
            prepTimeMinutes: 10,
            cookTimeMinutes: 40,
            instructions: "Bake bread.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Flour", 500m, "g", ShoppingCategory.Dried)]);

        var recipe2 = Recipe.Create(
            name: "Gravy",
            description: null,
            category: RecipeCategory.Side,
            servings: 1,
            prepTimeMinutes: 5,
            cookTimeMinutes: 10,
            instructions: "Make gravy.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Flour", 2m, "tbsp", ShoppingCategory.Dried)]);

        var entry1 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Lunch, recipe1.Id, 1);
        var entry2 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Dinner, recipe2.Id, 1);

        var result = _sut.Aggregate([(entry1, recipe1), (entry2, recipe2)]);

        result.Should().HaveCount(2);
        result.Should().Contain(i => i.Unit == "g" && i.TotalQuantity == 500m);
        result.Should().Contain(i => i.Unit == "tbsp" && i.TotalQuantity == 2m);
    }

    // Verifies that the FromRecipes list accurately records every recipe that
    // contributed to a given aggregated ingredient line, which allows the user to
    // trace back where each item on their shopping list is needed.
    [Test]
    public void Aggregate_FromRecipes_ContainsContributingRecipeNames()
    {
        var mealPlanId = Guid.NewGuid();

        var recipe1 = Recipe.Create(
            name: "Bolognese",
            description: null,
            category: RecipeCategory.Dinner,
            servings: 4,
            prepTimeMinutes: 10,
            cookTimeMinutes: 40,
            instructions: "Cook mince.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Tomato", 400m, "g", ShoppingCategory.FruitAndVeg)]);

        var recipe2 = Recipe.Create(
            name: "Tomato Soup",
            description: null,
            category: RecipeCategory.Lunch,
            servings: 2,
            prepTimeMinutes: 5,
            cookTimeMinutes: 20,
            instructions: "Blend tomatoes.",
            sourceUrl: null,
            ingredients: [new RecipeIngredient("Tomato", 600m, "g", ShoppingCategory.FruitAndVeg)]);

        var entry1 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Dinner, recipe1.Id, 4);
        var entry2 = MealPlanEntry.Create(mealPlanId, new DateOnly(2026, 3, 22), MealType.Lunch, recipe2.Id, 2);

        var result = _sut.Aggregate([(entry1, recipe1), (entry2, recipe2)]);

        result.Should().HaveCount(1);
        result[0].FromRecipes.Should().Contain("Bolognese");
        result[0].FromRecipes.Should().Contain("Tomato Soup");
        result[0].FromRecipes.Should().HaveCount(2);
    }

    // Verifies that passing an empty sequence of meal plan entries returns an empty
    // list rather than throwing, ensuring defensive handling of an edge case that can
    // occur when a meal plan has no entries yet.
    [Test]
    public void Aggregate_EmptyInput_ReturnsEmpty()
    {
        var result = _sut.Aggregate([]);

        result.Should().BeEmpty();
    }
}
