using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController(ILogger<ErrorController> logger) : ControllerBase
{
    [Route("/error")]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
        {
            logger.LogError(feature.Error, "Unhandled exception");
        }
        return Problem(statusCode: 500, title: "An unexpected error occurred.");
    }
}
