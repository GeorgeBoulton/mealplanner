using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/mealplans")]
public class MealPlansController : ControllerBase
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IShoppingListService _shoppingListService;

    public MealPlansController(IMealPlanService mealPlanService, IShoppingListService shoppingListService)
    {
        _mealPlanService = mealPlanService;
        _shoppingListService = shoppingListService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mealPlanService.GetAllAsync(HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var result = await _mealPlanService.GetOrCreateCurrentWeekAsync(HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mealPlanService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMealPlanRequest request)
    {
        var result = await _mealPlanService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mealPlanService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/entries")]
    public async Task<IActionResult> AddEntry(Guid id, [FromBody] MealPlanEntryRequest request)
    {
        var result = await _mealPlanService.AddEntryAsync(id, request, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid id, Guid entryId, [FromBody] MealPlanEntryRequest request)
    {
        var result = await _mealPlanService.UpdateEntryAsync(id, entryId, request, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> RemoveEntry(Guid id, Guid entryId)
    {
        await _mealPlanService.RemoveEntryAsync(id, entryId, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/shopping-list")]
    public async Task<IActionResult> GenerateShoppingList(Guid id)
    {
        var result = await _shoppingListService.GenerateAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}
