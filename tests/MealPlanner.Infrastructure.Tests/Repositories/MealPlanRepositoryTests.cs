using FluentAssertions;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Infrastructure.Repositories;

namespace MealPlanner.Infrastructure.Tests.Repositories;

[TestFixture]
public class MealPlanRepositoryTests : RepositoryTestBase
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static MealPlan BuildMealPlan(DateOnly? weekStart = null)
    {
        return MealPlan.Create(weekStart ?? new DateOnly(2026, 3, 23));
    }

    private static MealPlanEntry BuildEntry(Guid mealPlanId, DateOnly date)
    {
        return MealPlanEntry.Create(
            mealPlanId: mealPlanId,
            date: date,
            mealType: MealType.Dinner,
            recipeId: Guid.NewGuid(),
            servings: 4);
    }

    // -----------------------------------------------------------------------
    // AddAsync + GetByIdAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetById_WithValidId_ReturnsMealPlan()
    {
        // Verifies that a persisted meal plan is retrievable by its primary key
        // and that the Entries navigation collection is eagerly loaded.
        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        var mealPlan = BuildMealPlan();
        mealPlan.AddEntry(BuildEntry(mealPlan.Id, mealPlan.WeekStartDate));
        await repo.AddAsync(mealPlan, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new MealPlanRepository(readCtx);

        var result = await readRepo.GetByIdAsync(mealPlan.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(mealPlan.Id);
        result.WeekStartDate.Should().Be(mealPlan.WeekStartDate);
        result.Entries.Should().HaveCount(1);
    }

    [Test]
    public async Task GetById_WithMissingId_ReturnsNull()
    {
        // Verifies that querying a non-existent ID returns null rather than throwing.
        await using var ctx = CreateContext();
        var repo = new MealPlanRepository(ctx);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetByWeekAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetByWeek_WithMatchingDate_ReturnsMealPlan()
    {
        // Verifies that a meal plan can be looked up by its week-start date,
        // which is the primary natural key for the weekly planning workflow.
        var weekStart = new DateOnly(2026, 3, 23);

        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        var mealPlan = BuildMealPlan(weekStart);
        await repo.AddAsync(mealPlan, CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new MealPlanRepository(readCtx);

        var result = await readRepo.GetByWeekAsync(weekStart, CancellationToken.None);

        result.Should().NotBeNull();
        result!.WeekStartDate.Should().Be(weekStart);
    }

    [Test]
    public async Task GetByWeek_WithNonMatchingDate_ReturnsNull()
    {
        // Verifies that querying a week with no meal plan returns null,
        // allowing callers to detect "no plan yet" for a given week.
        var existingWeek = new DateOnly(2026, 3, 23);
        var missingWeek = new DateOnly(2026, 3, 30);

        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);
        await repo.AddAsync(BuildMealPlan(existingWeek), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new MealPlanRepository(readCtx);

        var result = await readRepo.GetByWeekAsync(missingWeek, CancellationToken.None);

        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetAll_WithMultipleMealPlans_ReturnsAll()
    {
        // Verifies that GetAll returns every stored meal plan without accidental filtering.
        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        await repo.AddAsync(BuildMealPlan(new DateOnly(2026, 3, 23)), CancellationToken.None);
        await repo.AddAsync(BuildMealPlan(new DateOnly(2026, 3, 30)), CancellationToken.None);
        await repo.AddAsync(BuildMealPlan(new DateOnly(2026, 4,  6)), CancellationToken.None);

        await using var readCtx = CreateContext();
        var readRepo = new MealPlanRepository(readCtx);

        var results = await readRepo.GetAllAsync(CancellationToken.None);

        results.Should().HaveCount(3);
    }

    [Test]
    public async Task GetAll_WithNoMealPlans_ReturnsEmptyList()
    {
        // Verifies an empty table returns an empty list, not null.
        await using var ctx = CreateContext();
        var repo = new MealPlanRepository(ctx);

        var results = await repo.GetAllAsync(CancellationToken.None);

        results.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Update_ChangesWeekStartDate_PersistsChange()
    {
        // Verifies that UpdateAsync correctly persists a change to the scalar property
        // WeekStartDate when the entity is mutated and saved via the same DbContext.
        // (Adding child entries via Update on a disconnected graph is not supported
        // by the repository's context.Update() pattern; entries must be added within
        // the same tracked context session.)
        var originalWeek = new DateOnly(2026, 3, 23);
        var updatedWeek = new DateOnly(2026, 3, 30);

        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        var mealPlan = BuildMealPlan(originalWeek);
        await repo.AddAsync(mealPlan, CancellationToken.None);

        // Load and mutate in the SAME context so EF change tracking works correctly
        await using var updateCtx = CreateContext();
        var updateRepo = new MealPlanRepository(updateCtx);
        var loaded = await updateRepo.GetByIdAsync(mealPlan.Id, CancellationToken.None);

        // Use reflection to set the private WeekStartDate (no domain update method exists)
        typeof(MealPlan)
            .GetProperty("WeekStartDate")!
            .SetValue(loaded, updatedWeek);

        await updateRepo.UpdateAsync(loaded!, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new MealPlanRepository(readCtx).GetByIdAsync(mealPlan.Id, CancellationToken.None);

        result!.WeekStartDate.Should().Be(updatedWeek);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task Delete_ExistingMealPlan_RemovesFromDatabase()
    {
        // Verifies that DeleteAsync removes the meal plan so a subsequent GetById returns null.
        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        var mealPlan = BuildMealPlan();
        await repo.AddAsync(mealPlan, CancellationToken.None);

        await repo.DeleteAsync(mealPlan.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new MealPlanRepository(readCtx).GetByIdAsync(mealPlan.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    public async Task Delete_NonExistentId_DoesNotThrow()
    {
        // Verifies that deleting an ID that doesn't exist is a no-op.
        await using var ctx = CreateContext();
        var repo = new MealPlanRepository(ctx);

        var act = async () => await repo.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Delete_MealPlanWithEntries_CascadesDelete()
    {
        // Verifies that deleting a plan also removes its entries so no orphaned
        // MealPlanEntry rows remain.
        await using var writeCtx = CreateContext();
        var repo = new MealPlanRepository(writeCtx);

        var mealPlan = BuildMealPlan(new DateOnly(2026, 3, 23));
        mealPlan.AddEntry(BuildEntry(mealPlan.Id, mealPlan.WeekStartDate));
        mealPlan.AddEntry(BuildEntry(mealPlan.Id, mealPlan.WeekStartDate.AddDays(1)));
        await repo.AddAsync(mealPlan, CancellationToken.None);

        await repo.DeleteAsync(mealPlan.Id, CancellationToken.None);

        await using var readCtx = CreateContext();
        var result = await new MealPlanRepository(readCtx).GetByIdAsync(mealPlan.Id, CancellationToken.None);
        result.Should().BeNull();

        // Confirm no orphaned entries remain in the table
        var entryCount = readCtx.MealPlanEntries.Count();
        entryCount.Should().Be(0);
    }
}
