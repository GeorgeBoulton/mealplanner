namespace MealPlanner.Domain.Entities;

public class FridgeItem
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public DateTime AddedAt { get; private set; }

    private FridgeItem()
    {
        Name = string.Empty;
    }

    public static FridgeItem Create(string name, decimal? quantity, string? unit)
    {
        return new FridgeItem
        {
            Id = Guid.NewGuid(),
            Name = name,
            Quantity = quantity,
            Unit = unit,
            AddedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, decimal? quantity, string? unit)
    {
        Name = name;
        Quantity = quantity;
        Unit = unit;
    }
}
