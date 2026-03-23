using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FridgeController : ControllerBase
{
    private readonly IFridgeService _fridgeService;

    public FridgeController(IFridgeService fridgeService)
    {
        _fridgeService = fridgeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _fridgeService.GetAllAsync(HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var result = await _fridgeService.GetSuggestionsAsync(HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _fridgeService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFridgeItemRequest request)
    {
        var result = await _fridgeService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFridgeItemRequest request)
    {
        var result = await _fridgeService.UpdateAsync(id, request, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _fridgeService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
