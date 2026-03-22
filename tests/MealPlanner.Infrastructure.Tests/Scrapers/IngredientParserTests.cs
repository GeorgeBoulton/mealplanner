using MealPlanner.Domain.Enums;
using MealPlanner.Infrastructure.Scrapers;
using NUnit.Framework;
using FluentAssertions;

namespace MealPlanner.Infrastructure.Tests.Scrapers;

// These tests verify the ingredient parser can handle common recipe string formats.
// The spec says "aim for 80% accuracy on common recipe sites" — these cover the
// most common patterns (quantity+unit+name, optional markers, fractional quantities).
[TestFixture]
public class IngredientParserTests
{
    [Test]
    public void Parse_SimpleGramWeight_ExtractsCorrectly()
    {
        // Verifies basic "500g beef mince" format used by BBC Good Food etc.
        var result = IngredientParser.Parse("500g beef mince");

        result.Quantity.Should().Be(500m);
        result.Unit.Should().Be("g");
        result.Name.Should().Be("beef mince");
    }

    [Test]
    public void Parse_TablespoonUnit_ExtractsCorrectly()
    {
        var result = IngredientParser.Parse("2 tablespoons olive oil");

        result.Quantity.Should().Be(2m);
        result.Unit.Should().Be("tablespoons");
        result.Name.Should().Be("olive oil");
    }

    [Test]
    public void Parse_TbspAbbreviation_ExtractsCorrectly()
    {
        var result = IngredientParser.Parse("1 tbsp soy sauce");

        result.Quantity.Should().Be(1m);
        result.Unit.Should().Be("tbsp");
    }

    [Test]
    public void Parse_FractionalQuantity_ExtractsCorrectly()
    {
        // Fractions like "1/2 cup" are common in US recipes
        var result = IngredientParser.Parse("1/2 cup flour");

        result.Quantity.Should().BeApproximately(0.5m, 0.001m);
        result.Unit.Should().Be("cup");
    }

    [Test]
    public void Parse_NoQuantity_SetsQuantityToZero()
    {
        // Some ingredients like "salt" have no quantity
        var result = IngredientParser.Parse("Salt and pepper to taste");

        result.Quantity.Should().Be(0m);
        result.Optional.Should().BeTrue();
    }

    [Test]
    public void Parse_OptionalMarker_SetsOptionalTrue()
    {
        // "optional" keyword should flag the ingredient
        var result = IngredientParser.Parse("1 handful parsley, optional");

        result.Optional.Should().BeTrue();
    }

    [Test]
    public void Parse_GarnishMarker_SetsOptionalTrue()
    {
        var result = IngredientParser.Parse("fresh basil, garnish");

        result.Optional.Should().BeTrue();
    }

    [Test]
    public void Parse_BeefIngredient_ClassifiedAsMeat()
    {
        var result = IngredientParser.Parse("500g beef mince");

        result.ShoppingCategory.Should().Be(ShoppingCategory.Meat);
    }

    [Test]
    public void Parse_SalmonIngredient_ClassifiedAsFish()
    {
        var result = IngredientParser.Parse("200g salmon fillet");

        result.ShoppingCategory.Should().Be(ShoppingCategory.Fish);
    }

    [Test]
    public void Parse_TomatoIngredient_ClassifiedAsFruitAndVeg()
    {
        var result = IngredientParser.Parse("2 tomatoes, diced");

        result.ShoppingCategory.Should().Be(ShoppingCategory.FruitAndVeg);
    }

    [Test]
    public void Parse_MilkIngredient_ClassifiedAsDairy()
    {
        var result = IngredientParser.Parse("200ml milk");

        result.ShoppingCategory.Should().Be(ShoppingCategory.Dairy);
    }

    [Test]
    public void Parse_RangeQuantity_TakesFirstNumber()
    {
        // Ranges like "1-2 onions" — take first value
        var result = IngredientParser.Parse("1-2 onions");

        result.Quantity.Should().Be(1m);
    }

    [Test]
    public void Parse_RawIsPreserved()
    {
        // The raw string must always be preserved for display/edit fallback
        const string raw = "2 tablespoons olive oil";
        var result = IngredientParser.Parse(raw);

        result.Raw.Should().Be(raw);
    }
}
