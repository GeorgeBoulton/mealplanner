namespace MealPlanner.Domain.Entities;

public class ShoppingList
{
    public Guid Id { get; private set; }
    public Guid MealPlanId { get; private set; }

    private readonly List<ShoppingListItem> _items = new();
    public IReadOnlyList<ShoppingListItem> Items => _items;

    public DateTime GeneratedAt { get; private set; }

    private ShoppingList() { }

    public static ShoppingList Create(Guid mealPlanId, IEnumerable<ShoppingListItem> items)
    {
        var list = new ShoppingList
        {
            Id = Guid.NewGuid(),
            MealPlanId = mealPlanId,
            GeneratedAt = DateTime.UtcNow
        };

        list._items.AddRange(items);

        return list;
    }
}
