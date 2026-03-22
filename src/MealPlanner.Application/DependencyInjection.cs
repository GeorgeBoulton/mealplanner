using MealPlanner.Application.Interfaces;
using MealPlanner.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        return services;
    }
}
