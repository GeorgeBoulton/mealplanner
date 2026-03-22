using MealPlanner.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace MealPlanner.Infrastructure.Tests.Repositories;

// Base class for all repository integration tests.
// Spins up a real PostgreSQL container once per test class (OneTimeSetUp/OneTimeSetDown)
// and truncates all tables before each individual test so every test starts clean.
[TestFixture]
public abstract class RepositoryTestBase
{
    private PostgreSqlContainer _container = null!;

    [OneTimeSetUp]
    public async Task StartContainer()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();

        await _container.StartAsync();

        // Apply migrations once for the lifetime of this test fixture
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task StopContainer()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task ClearDatabase()
    {
        // Truncate all tables before each test so tests are fully isolated.
        // CASCADE handles FK dependencies automatically.
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"RecipeIngredients\", \"Recipes\", \"MealPlanEntries\", \"MealPlans\", \"ShoppingListItems\", \"ShoppingLists\", \"FridgeItems\" CASCADE");
    }

    protected MealPlannerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new MealPlannerDbContext(options);
    }
}
