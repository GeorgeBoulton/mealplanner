namespace MealPlanner.Domain.Entities;

public class MealPlan
{
    public Guid Id { get; private set; }
    public DateOnly WeekStartDate { get; private set; }

    private readonly List<MealPlanEntry> _entries = new();
    public IReadOnlyList<MealPlanEntry> Entries => _entries;

    public DateTime CreatedAt { get; private set; }

    private MealPlan() { }

    public static MealPlan Create(DateOnly weekStartDate)
    {
        return new MealPlan
        {
            Id = Guid.NewGuid(),
            WeekStartDate = weekStartDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddEntry(MealPlanEntry entry)
    {
        _entries.Add(entry);
    }

    public void RemoveEntry(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
            _entries.Remove(entry);
    }
}
