using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Infrastructure.Repositories;

namespace MealPlanner.Infrastructure.Tests.Repositories;

[TestFixture]
public class ShoppingListRepositoryTests : RepositoryTestBase
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ShoppingList BuildShoppingList(Guid? mealPlanId = null, int itemCount = 2)
    {
        var mpId = mealPlanId ?? Guid.NewGuid();
        var items = Enumerable.Range(1, itemCount)
            .Select(i => ShoppingListItem.Create(
                shoppingListId: Guid.Empty, // EF will set FK via the navigation
                ingredientName: $"Ingredient {i}",
                totalQuantity: i * 100m,
                unit: "g",
                category: ShoppingCategory.Other,
                fromRecipes: new[] { "Test Recipe" }))
            .ToList();

        return ShoppingList.Create(mpId, items);
    }

    // -----------------------------------------------------------------------
    // AddAsync + GetByIdAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetById_WithValidId_ReturnsShoppingListWithItems()
    {
        // Verifies that a persisted shopping list can be retrieved by ID and
        // that its Items navigation collection is eagerly included.
        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);

        var list = BuildShoppingList(itemCount: 3);
        await repo.AddAsync(list, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new ShoppingListRepository(readCtx);

        var result = await readRepo.GetByIdAsync(list.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(list.Id);
        result.Items.Should().HaveCount(3);
    }

    [Test]
    public async Task GetById_WithMissingId_ReturnsNull()
    {
        // Verifies that a non-existent ID yields null rather than an exception.
        await using var ctx = CreateContext();
        var repo = new ShoppingListRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetByMealPlanAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetByMealPlan_WithMatchingMealPlanId_ReturnsShoppingList()
    {
        // Verifies that the shopping list for a given meal plan can be found
        // by the meal plan FK, supporting the "generate list for week" use case.
        var mealPlanId = Guid.NewGuid();

        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);

        var list = BuildShoppingList(mealPlanId);
        await repo.AddAsync(list, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new ShoppingListRepository(readCtx);

        var result = await readRepo.GetByMealPlanAsync(mealPlanId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.MealPlanId.Should().Be(mealPlanId);
    }

    [Test]
    public async Task GetByMealPlan_WithNonMatchingId_ReturnsNull()
    {
        // Verifies that querying by a meal plan ID that has no shopping list returns null.
        var realMealPlanId = Guid.NewGuid();
        var otherMealPlanId = Guid.NewGuid();

        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);
        await repo.AddAsync(BuildShoppingList(realMealPlanId), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new ShoppingListRepository(readCtx);

        var result = await readRepo.GetByMealPlanAsync(otherMealPlanId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetByMealPlan_IncludesItems()
    {
        // Verifies that GetByMealPlan also eagerly loads the Items collection,
        // not just the ShoppingList header row.
        var mealPlanId = Guid.NewGuid();

        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);
        await repo.AddAsync(BuildShoppingList(mealPlanId, itemCount: 4), CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new ShoppingListRepository(readCtx)
            .GetByMealPlanAsync(mealPlanId, CancellationToken.None);

        result!.Items.Should().HaveCount(4);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Update_TogglesItemChecked_PersistsChange()
    {
        // Verifies that mutating an item (toggle IsChecked) and calling Update
        // writes the change through to the database.
        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);

        var list = BuildShoppingList(itemCount: 1);
        await repo.AddAsync(list, CancellationToken.None);

        // Reload, toggle, save
        await using var updateCtx = CreateContext();
        var updateRepo = new ShoppingListRepository(updateCtx);
        var loaded = await updateRepo.GetByIdAsync(list.Id, CancellationToken.None);
        loaded!.Items[0].ToggleChecked();
        await updateRepo.UpdateAsync(loaded, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new ShoppingListRepository(readCtx).GetByIdAsync(list.Id, CancellationToken.None);

        result!.Items[0].IsChecked.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Delete_ExistingShoppingList_RemovesFromDatabase()
    {
        // Verifies that DeleteAsync removes the shopping list so a subsequent
        // GetById returns null.
        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);

        var list = BuildShoppingList();
        await repo.AddAsync(list, CancellationToken.None);

        await repo.DeleteAsync(list.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new ShoppingListRepository(readCtx).GetByIdAsync(list.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task Delete_NonExistentId_DoesNotThrow()
    {
        // Verifies that deleting a non-existent ID is a silent no-op.
        await using var ctx = CreateContext();
        var repo = new ShoppingListRepository(ctx);

        var act = async () => await repo.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Delete_ShoppingListWithItems_CascadesDelete()
    {
        // Verifies that cascade delete removes all ShoppingListItem rows when
        // the parent ShoppingList is deleted, leaving no orphans.
        await using var writeCtx = CreateContext();
        var repo = new ShoppingListRepository(writeCtx);

        var list = BuildShoppingList(itemCount: 5);
        await repo.AddAsync(list, CancellationToken.None);

        await repo.DeleteAsync(list.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var itemCount = readCtx.ShoppingListItems.Count();
        itemCount.Should().Be(0);
    }
}
