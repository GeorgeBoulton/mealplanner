using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? tag,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        RecipeCategory? parsedCategory = null;
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<RecipeCategory>(category, true, out var cat))
        {
            parsedCategory = cat;
        }

        var result = await _recipeService.GetAllAsync(search, parsedCategory, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var result = await _recipeService.GetSuggestionsAsync(HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _recipeService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest request)
    {
        var result = await _recipeService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecipeRequest request)
    {
        var result = await _recipeService.UpdateAsync(id, request, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _recipeService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportRecipeRequest request)
    {
        var result = await _recipeService.ImportAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
