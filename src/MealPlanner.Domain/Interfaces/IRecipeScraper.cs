using MealPlanner.Domain.Models;

namespace MealPlanner.Domain.Interfaces;

public interface IRecipeScraper
{
    Task<ScrapedRecipe> ScrapeAsync(string url, CancellationToken ct = default);
}
