using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Repositories;

namespace MealPlanner.Infrastructure.Tests.Repositories;

[TestFixture]
public class FridgeRepositoryTests : RepositoryTestBase
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static FridgeItem BuildFridgeItem(string name = "Milk", decimal quantity = 2m, string unit = "litres")
    {
        return FridgeItem.Create(name, quantity, unit);
    }

    // -----------------------------------------------------------------------
    // AddAsync + GetByIdAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetById_WithValidId_ReturnsFridgeItem()
    {
        // Verifies that a fridge item can be retrieved by its primary key
        // with all properties correctly persisted.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        var item = BuildFridgeItem("Eggs", 12m, "units");
        await repo.AddAsync(item, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new FridgeRepository(readCtx);

        var result = await readRepo.GetByIdAsync(item.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(item.Id);
        result.Name.Should().Be("Eggs");
        result.Quantity.Should().Be(12m);
        result.Unit.Should().Be("units");
    }

    [Test]
    public async Task GetById_WithMissingId_ReturnsNull()
    {
        // Verifies that querying a non-existent fridge item returns null.
        await using var ctx = CreateContext();
        var repo = new FridgeRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetById_WithNullableQuantity_RoundTripsCorrectly()
    {
        // Verifies that nullable Quantity (null means "some, unspecified amount")
        // is preserved through insert and retrieval.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        var item = FridgeItem.Create("Salt", null, null);
        await repo.AddAsync(item, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new FridgeRepository(readCtx).GetByIdAsync(item.Id, CancellationToken.None);

        result!.Quantity.Should().BeNull();
        result.Unit.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetAll_WithMultipleItems_ReturnsAll()
    {
        // Verifies that GetAll returns every fridge item without accidental filtering.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        await repo.AddAsync(BuildFridgeItem("Milk"), CancellationToken.None);
        await repo.AddAsync(BuildFridgeItem("Butter"), CancellationToken.None);
        await repo.AddAsync(BuildFridgeItem("Cheese"), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new FridgeRepository(readCtx);

        var results = await readRepo.GetAllAsync(CancellationToken.None);

        results.Should().HaveCount(3);
    }

    [Test]
    public async Task GetAll_WithNoItems_ReturnsEmptyList()
    {
        // Verifies that an empty fridge table returns an empty list, not null.
        await using var ctx = CreateContext();
        var repo = new FridgeRepository(ctx);

        var results = await repo.GetAllAsync(CancellationToken.None);

        results.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Update_ExistingFridgeItem_PersistsChanges()
    {
        // Verifies that after an update, the new field values are stored
        // and visible when the item is re-loaded from the database.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        var item = BuildFridgeItem("Milk", 2m, "litres");
        await repo.AddAsync(item, CancellationToken.None);

        // FridgeItem has no Update method — create a replacement to simulate an update
        // via the repository's Update path using a detached entity with the same ID.
        // Instead, load the entity fresh and replace via UpdateAsync on the same context.
        await using var updateCtx = CreateContext();
        var updateRepo = new FridgeRepository(updateCtx);
        var loaded = await updateRepo.GetByIdAsync(item.Id, CancellationToken.None);

        // Use reflection to set private properties since there is no domain Update method
        typeof(FridgeItem)
            .GetProperty("Quantity")!
            .SetValue(loaded, 1.5m);
        typeof(FridgeItem)
            .GetProperty("Unit")!
            .SetValue(loaded, "litres (updated)");

        await updateRepo.UpdateAsync(loaded!, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new FridgeRepository(readCtx).GetByIdAsync(item.Id, CancellationToken.None);

        result!.Quantity.Should().Be(1.5m);
        result.Unit.Should().Be("litres (updated)");
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Delete_ExistingFridgeItem_RemovesFromDatabase()
    {
        // Verifies that DeleteAsync removes the entity so a subsequent GetById returns null.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        var item = BuildFridgeItem("Old Yoghurt");
        await repo.AddAsync(item, CancellationToken.None);

        await repo.DeleteAsync(item.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new FridgeRepository(readCtx).GetByIdAsync(item.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task Delete_NonExistentId_DoesNotThrow()
    {
        // Verifies that deleting a non-existent fridge item is a silent no-op.
        await using var ctx = CreateContext();
        var repo = new FridgeRepository(ctx);

        var act = async () => await repo.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Delete_ReducesGetAllCount()
    {
        // End-to-end sanity check: add two items, delete one, confirm only one remains.
        await using var writeCtx = CreateContext();
        var repo = new FridgeRepository(writeCtx);

        var item1 = BuildFridgeItem("Apple Juice");
        var item2 = BuildFridgeItem("Orange Juice");
        await repo.AddAsync(item1, CancellationToken.None);
        await repo.AddAsync(item2, CancellationToken.None);

        await repo.DeleteAsync(item1.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var results = await new FridgeRepository(readCtx).GetAllAsync(CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Orange Juice");
    }
}
