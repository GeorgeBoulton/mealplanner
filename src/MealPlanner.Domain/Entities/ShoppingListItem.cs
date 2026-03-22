using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.Entities;

public class ShoppingListItem
{
    public Guid Id { get; private set; }
    public Guid ShoppingListId { get; private set; }
    public string IngredientName { get; private set; }
    public decimal TotalQuantity { get; private set; }
    public string Unit { get; private set; }
    public ShoppingCategory Category { get; private set; }
    public bool IsChecked { get; private set; }

    private readonly List<string> _fromRecipes = new();
    public IReadOnlyList<string> FromRecipes => _fromRecipes;

    private ShoppingListItem()
    {
        IngredientName = string.Empty;
        Unit = string.Empty;
    }

    public static ShoppingListItem Create(
        Guid shoppingListId,
        string ingredientName,
        decimal totalQuantity,
        string unit,
        ShoppingCategory category,
        IEnumerable<string>? fromRecipes = null)
    {
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = shoppingListId,
            IngredientName = ingredientName,
            TotalQuantity = totalQuantity,
            Unit = unit,
            Category = category,
            IsChecked = false
        };

        if (fromRecipes != null)
            item._fromRecipes.AddRange(fromRecipes);

        return item;
    }

    public void ToggleChecked()
    {
        IsChecked = !IsChecked;
    }
}
