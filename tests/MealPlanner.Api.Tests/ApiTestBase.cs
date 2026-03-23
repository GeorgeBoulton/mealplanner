using System.Collections.Generic;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MealPlanner.Api.Tests;

/// <summary>
/// Base class for all API integration tests. Spins up a real PostgreSQL container via
/// Testcontainers, creates a custom WebApplicationFactory that points the app at that
/// container, runs EF migrations, and truncates all tables before every individual test
/// so each test starts from a clean slate.
/// </summary>
[TestFixture]
public abstract class ApiTestBase
{
    private PostgreSqlContainer _container = null!;
    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task StartContainerAndFactory()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();

        await _container.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
                });
            });
        });

        // Migrations are applied automatically by Program.cs on startup.
        Client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task StopContainerAndFactory()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task ClearDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MealPlannerDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"RecipeIngredients\", \"Recipes\", \"MealPlanEntries\", \"MealPlans\", \"ShoppingListItems\", \"ShoppingLists\", \"FridgeItems\" CASCADE");
    }
}
