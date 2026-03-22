using MealPlanner.Domain.Enums;

namespace MealPlanner.Domain.Entities;

public class MealPlanEntry
{
    public Guid Id { get; private set; }
    public Guid MealPlanId { get; private set; }
    public DateOnly Date { get; private set; }
    public MealType MealType { get; private set; }
    public Guid RecipeId { get; private set; }
    public int Servings { get; private set; }

    private MealPlanEntry() { }

    public static MealPlanEntry Create(
        Guid mealPlanId,
        DateOnly date,
        MealType mealType,
        Guid recipeId,
        int servings)
    {
        return new MealPlanEntry
        {
            Id = Guid.NewGuid(),
            MealPlanId = mealPlanId,
            Date = date,
            MealType = mealType,
            RecipeId = recipeId,
            Servings = servings
        };
    }
}
