using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealPlanner.Infrastructure.Data;

public class MealPlannerDbContextFactory : IDesignTimeDbContextFactory<MealPlannerDbContext>
{
    public MealPlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseNpgsql("Host=localhost;Database=mealplanner;Username=mealplanner;Password=mealplanner_dev")
            .Options;

        return new MealPlannerDbContext(options);
    }
}
