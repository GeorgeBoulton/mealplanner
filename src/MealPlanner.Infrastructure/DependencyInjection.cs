using MealPlanner.Domain.Interfaces;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Infrastructure.Repositories;
using MealPlanner.Infrastructure.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MealPlannerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IFridgeRepository, FridgeRepository>();

        services.AddHttpClient("RecipeScraper", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "MealPlannerBot/1.0 (+https://github.com/mealplanner)");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IRecipeScraper, RecipeScraper>();

        return services;
    }
}
