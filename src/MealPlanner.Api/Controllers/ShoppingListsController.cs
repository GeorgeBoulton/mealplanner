using MealPlanner.Application.DTOs;
using MealPlanner.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/shopping-lists")]
public class ShoppingListsController : ControllerBase
{
    private readonly IShoppingListService _shoppingListService;

    public ShoppingListsController(IShoppingListService shoppingListService)
    {
        _shoppingListService = shoppingListService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _shoppingListService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateShoppingListItemRequest request)
    {
        var result = await _shoppingListService.UpdateItemAsync(id, itemId, request, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _shoppingListService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        var result = await _shoppingListService.ExportAsync(id, HttpContext.RequestAborted);
        if (result is null)
            return NotFound();
        return Content(result, "text/plain");
    }
}
