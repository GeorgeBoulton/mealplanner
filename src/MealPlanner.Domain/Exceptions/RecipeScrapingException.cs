namespace MealPlanner.Domain.Exceptions;

public class RecipeScrapingException : Exception
{
    public RecipeScrapingException(string message) : base(message) { }
}
