using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.ValueObjects;

public class RecipeIngredient
{
    public string Name { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; }
    public ShoppingCategory ShoppingCategory { get; private set; }
    public bool Optional { get; private set; }

    public RecipeIngredient(string name, decimal quantity, string unit, ShoppingCategory shoppingCategory, bool optional = false)
    {
        Name = name;
        Quantity = quantity;
        Unit = unit;
        ShoppingCategory = shoppingCategory;
        Optional = optional;
    }

    // Parameterless constructor for EF Core
    private RecipeIngredient()
    {
        Name = string.Empty;
        Unit = string.Empty;
    }
}
