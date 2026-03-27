using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Interfaces;
using DomainEnums = MealPlanner.Domain.Enums;

namespace MealPlanner.Application.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public MealPlanService(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<IReadOnlyList<MealPlanResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var mealPlans = await _mealPlanRepository.GetAllAsync(ct);
        return mealPlans.Select(MapToResponse).ToList();
    }

    public async Task<MealPlanResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(id, ct);
        return mealPlan is null ? null : MapToResponse(mealPlan);
    }

    public async Task<MealPlanResponse> GetOrCreateCurrentWeekAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monday = GetMonday(today);

        var mealPlan = await _mealPlanRepository.GetByWeekAsync(monday, ct);
        if (mealPlan is not null)
            return MapToResponse(mealPlan);

        var created = MealPlan.Create(monday);
        await _mealPlanRepository.AddAsync(created, ct);
        return MapToResponse(created);
    }

    public async Task<MealPlanResponse> CreateAsync(CreateMealPlanRequest request, CancellationToken ct = default)
    {
        var mealPlan = MealPlan.Create(request.WeekStartDate);
        await _mealPlanRepository.AddAsync(mealPlan, ct);
        return MapToResponse(mealPlan);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _mealPlanRepository.DeleteAsync(id, ct);
    }

    public async Task<MealPlanResponse?> AddEntryAsync(Guid id, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(id, ct);
        if (mealPlan is null)
            return null;

        ValidateEntryDate(request.Date, mealPlan.WeekStartDate);
        ValidateServings(request.Servings);

        var entry = MealPlanEntry.Create(mealPlan.Id, request.Date, (DomainEnums.MealType)(int)request.MealType, request.RecipeId, request.Servings);
        mealPlan.AddEntry(entry);
        await _mealPlanRepository.UpdateAsync(mealPlan, ct);
        return MapToResponse(mealPlan);
    }

    public async Task<MealPlanResponse?> UpdateEntryAsync(Guid id, Guid entryId, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(id, ct);
        if (mealPlan is null)
            return null;

        ValidateEntryDate(request.Date, mealPlan.WeekStartDate);
        ValidateServings(request.Servings);

        mealPlan.RemoveEntry(entryId);
        var entry = MealPlanEntry.Create(mealPlan.Id, request.Date, (DomainEnums.MealType)(int)request.MealType, request.RecipeId, request.Servings);
        mealPlan.AddEntry(entry);
        await _mealPlanRepository.UpdateAsync(mealPlan, ct);
        return MapToResponse(mealPlan);
    }

    public async Task<MealPlanResponse?> RemoveEntryAsync(Guid id, Guid entryId, CancellationToken ct = default)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(id, ct);
        if (mealPlan is null)
            return null;

        mealPlan.RemoveEntry(entryId);
        await _mealPlanRepository.UpdateAsync(mealPlan, ct);
        return MapToResponse(mealPlan);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysFromMonday);
    }

    private static void ValidateEntryDate(DateOnly date, DateOnly weekStartDate)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        if (date < weekStartDate || date > weekEndDate)
            throw new ArgumentException(
                $"Entry date {date} must fall within the meal plan's week ({weekStartDate} to {weekEndDate}).");
    }

    private static void ValidateServings(int servings)
    {
        if (servings < 1)
            throw new ArgumentException("Servings must be at least 1.");
    }

    private static MealPlanResponse MapToResponse(MealPlan mealPlan)
        => new(
            mealPlan.Id,
            mealPlan.WeekStartDate,
            mealPlan.Entries
                .Select(e => new MealPlanEntryResponse(e.Id, e.Date, (MealType)(int)e.MealType, e.RecipeId, e.Servings))
                .ToList(),
            mealPlan.CreatedAt);
}
