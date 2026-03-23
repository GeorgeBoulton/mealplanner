using MealPlanner.Application.Interfaces;
using MealPlanner.Application.Services;
using MealPlanner.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IngredientAggregator>();
        return services;
    }
}
