using FluentAssertions;
using MealPlanner.Application.DTOs;
using MealPlanner.Application.Services;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Enums;
using MealPlanner.Domain.Interfaces;
using NSubstitute;

namespace MealPlanner.Application.Tests;

[TestFixture]
public class MealPlanServiceTests
{
    private IMealPlanRepository _mealPlanRepository = null!;
    private MealPlanService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mealPlanRepository = Substitute.For<IMealPlanRepository>();
        _sut = new MealPlanService(_mealPlanRepository);
    }

    // -- helpers --

    private static MealPlan BuildMealPlan(DateOnly? weekStartDate = null)
    {
        var date = weekStartDate ?? new DateOnly(2025, 3, 17); // a Monday
        return MealPlan.Create(date);
    }

    private static MealPlanEntryRequest BuildEntryRequest(
        DateOnly? date = null,
        MealType mealType = MealType.Dinner,
        int servings = 2)
    {
        var entryDate = date ?? new DateOnly(2025, 3, 17);
        return new MealPlanEntryRequest(entryDate, mealType, Guid.NewGuid(), servings);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetAllAsync delegates to the repository and maps all
    /// returned meal plans to MealPlanResponse objects.
    /// </summary>
    [Test]
    public async Task GetAllAsync_ShouldReturnMappedResponses_WhenMealPlansExist()
    {
        var mealPlan = BuildMealPlan();
        _mealPlanRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan> { mealPlan });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(mealPlan.Id);
        result[0].WeekStartDate.Should().Be(mealPlan.WeekStartDate);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns an empty list when no meal plans exist.
    /// </summary>
    [Test]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoMealPlansExist()
    {
        _mealPlanRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<MealPlan>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null when the repository cannot find
    /// a meal plan with the given id.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMealPlanNotFound()
    {
        _mealPlanRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetByIdAsync maps the found meal plan to a MealPlanResponse
    /// with all fields correctly transferred.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ShouldReturnMealPlan_WhenFound()
    {
        var mealPlan = BuildMealPlan();
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var result = await _sut.GetByIdAsync(mealPlan.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(mealPlan.Id);
        result.WeekStartDate.Should().Be(mealPlan.WeekStartDate);
        result.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetOrCreateCurrentWeekAsync returns the existing meal plan
    /// when one already exists for the current week.
    /// </summary>
    [Test]
    public async Task GetOrCreateCurrentWeekAsync_ShouldReturnExisting_WhenCurrentWeekPlanExists()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var existing = MealPlan.Create(monday);
        _mealPlanRepository.GetByWeekAsync(monday, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.GetOrCreateCurrentWeekAsync();

        result.Id.Should().Be(existing.Id);
        await _mealPlanRepository.DidNotReceive().AddAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that GetOrCreateCurrentWeekAsync creates and persists a new meal plan
    /// when none exists for the current week.
    /// </summary>
    [Test]
    public async Task GetOrCreateCurrentWeekAsync_ShouldCreateAndPersist_WhenNoCurrentWeekPlanExists()
    {
        _mealPlanRepository.GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.GetOrCreateCurrentWeekAsync();

        await _mealPlanRepository.Received(1).AddAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        // Week start date must be a Monday
        result.WeekStartDate.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    /// <summary>
    /// Verifies that CreateAsync persists a new MealPlan via the repository and
    /// returns a response reflecting the supplied week start date.
    /// </summary>
    [Test]
    public async Task CreateAsync_ShouldCreateAndReturnMealPlan()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var request = new CreateMealPlanRequest(weekStart);

        var result = await _sut.CreateAsync(request);

        await _mealPlanRepository.Received(1).AddAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
        result.WeekStartDate.Should().Be(weekStart);
        result.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that DeleteAsync calls the repository with the correct id.
    /// </summary>
    [Test]
    public async Task DeleteAsync_ShouldCallRepository()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteAsync(id);

        await _mealPlanRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that AddEntryAsync returns null when the meal plan does not exist.
    /// </summary>
    [Test]
    public async Task AddEntryAsync_ShouldReturnNull_WhenMealPlanNotFound()
    {
        _mealPlanRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.AddEntryAsync(Guid.NewGuid(), BuildEntryRequest());

        result.Should().BeNull();
        await _mealPlanRepository.DidNotReceive().UpdateAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that AddEntryAsync throws ArgumentException when the entry date
    /// falls outside the meal plan's week.
    /// </summary>
    [Test]
    public async Task AddEntryAsync_ShouldThrowArgumentException_WhenDateOutsideWeek()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var request = BuildEntryRequest(date: new DateOnly(2025, 3, 24)); // next Monday, outside week

        Func<Task> act = () => _sut.AddEntryAsync(mealPlan.Id, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that AddEntryAsync throws ArgumentException when servings is less than 1.
    /// </summary>
    [Test]
    public async Task AddEntryAsync_ShouldThrowArgumentException_WhenServingsLessThanOne()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var request = BuildEntryRequest(date: weekStart, servings: 0);

        Func<Task> act = () => _sut.AddEntryAsync(mealPlan.Id, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that AddEntryAsync adds the entry to the meal plan, persists it,
    /// and returns the updated response including the new entry.
    /// </summary>
    [Test]
    public async Task AddEntryAsync_ShouldAddEntryAndReturnResponse_WhenValid()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var request = BuildEntryRequest(date: weekStart, mealType: MealType.Lunch, servings: 3);

        var result = await _sut.AddEntryAsync(mealPlan.Id, request);

        await _mealPlanRepository.Received(1).UpdateAsync(mealPlan, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Entries.Should().HaveCount(1);
        result.Entries[0].MealType.Should().Be(MealType.Lunch);
        result.Entries[0].Servings.Should().Be(3);
        result.Entries[0].Date.Should().Be(weekStart);
    }

    /// <summary>
    /// Verifies that AddEntryAsync accepts the last day (Sunday) of the week.
    /// </summary>
    [Test]
    public async Task AddEntryAsync_ShouldAcceptLastDayOfWeek()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var sunday = weekStart.AddDays(6);
        var mealPlan = BuildMealPlan(weekStart);
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var request = BuildEntryRequest(date: sunday);

        var result = await _sut.AddEntryAsync(mealPlan.Id, request);

        result.Should().NotBeNull();
        result!.Entries.Should().HaveCount(1);
        result.Entries[0].Date.Should().Be(sunday);
    }

    /// <summary>
    /// Verifies that UpdateEntryAsync returns null when the meal plan does not exist.
    /// </summary>
    [Test]
    public async Task UpdateEntryAsync_ShouldReturnNull_WhenMealPlanNotFound()
    {
        _mealPlanRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.UpdateEntryAsync(Guid.NewGuid(), Guid.NewGuid(), BuildEntryRequest());

        result.Should().BeNull();
        await _mealPlanRepository.DidNotReceive().UpdateAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that UpdateEntryAsync removes the old entry and adds a new one
    /// with the updated values, then persists and returns the updated response.
    /// </summary>
    [Test]
    public async Task UpdateEntryAsync_ShouldReplaceEntryAndReturnResponse_WhenValid()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);

        // Add an initial entry to the meal plan
        var initialEntry = MealPlanEntry.Create(mealPlan.Id, weekStart, MealType.Breakfast, Guid.NewGuid(), 1);
        mealPlan.AddEntry(initialEntry);

        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var newRecipeId = Guid.NewGuid();
        var updateRequest = new MealPlanEntryRequest(weekStart.AddDays(1), MealType.Dinner, newRecipeId, 4);

        var result = await _sut.UpdateEntryAsync(mealPlan.Id, initialEntry.Id, updateRequest);

        await _mealPlanRepository.Received(1).UpdateAsync(mealPlan, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Entries.Should().HaveCount(1);
        result.Entries[0].MealType.Should().Be(MealType.Dinner);
        result.Entries[0].Servings.Should().Be(4);
        result.Entries[0].RecipeId.Should().Be(newRecipeId);
    }

    /// <summary>
    /// Verifies that UpdateEntryAsync throws ArgumentException when the new
    /// entry date is outside the meal plan's week.
    /// </summary>
    [Test]
    public async Task UpdateEntryAsync_ShouldThrowArgumentException_WhenDateOutsideWeek()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var request = BuildEntryRequest(date: weekStart.AddDays(7)); // one week later

        Func<Task> act = () => _sut.UpdateEntryAsync(mealPlan.Id, Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that RemoveEntryAsync returns null when the meal plan does not exist.
    /// </summary>
    [Test]
    public async Task RemoveEntryAsync_ShouldReturnNull_WhenMealPlanNotFound()
    {
        _mealPlanRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MealPlan?)null);

        var result = await _sut.RemoveEntryAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
        await _mealPlanRepository.DidNotReceive().UpdateAsync(Arg.Any<MealPlan>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that RemoveEntryAsync removes the specified entry, persists
    /// the change, and returns the updated meal plan response without the entry.
    /// </summary>
    [Test]
    public async Task RemoveEntryAsync_ShouldRemoveEntryAndReturnResponse_WhenValid()
    {
        var weekStart = new DateOnly(2025, 3, 17);
        var mealPlan = BuildMealPlan(weekStart);

        var entry = MealPlanEntry.Create(mealPlan.Id, weekStart, MealType.Lunch, Guid.NewGuid(), 2);
        mealPlan.AddEntry(entry);

        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var result = await _sut.RemoveEntryAsync(mealPlan.Id, entry.Id);

        await _mealPlanRepository.Received(1).UpdateAsync(mealPlan, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Entries.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that RemoveEntryAsync still succeeds and persists when the
    /// entry id does not exist (RemoveEntry is a no-op in that case).
    /// </summary>
    [Test]
    public async Task RemoveEntryAsync_ShouldStillPersist_WhenEntryIdNotFound()
    {
        var mealPlan = BuildMealPlan();
        _mealPlanRepository.GetByIdAsync(mealPlan.Id, Arg.Any<CancellationToken>())
            .Returns(mealPlan);

        var result = await _sut.RemoveEntryAsync(mealPlan.Id, Guid.NewGuid());

        await _mealPlanRepository.Received(1).UpdateAsync(mealPlan, Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result!.Entries.Should().BeEmpty();
    }
}
