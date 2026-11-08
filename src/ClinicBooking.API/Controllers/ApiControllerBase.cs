using System.Security.Claims;
using ClinicBooking.Application.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected Guid? CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapErrorToActionResult(result.Error);
    }

    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return MapErrorToActionResult(result.Error);
    }

    private ActionResult MapErrorToActionResult(Error error)
    {
        var statusCode = error.Code switch
        {
            var code when code.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
            var code when code.StartsWith("Auth.Unauthorized", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status401Unauthorized,
            var code when code.StartsWith("Auth.Forbidden", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status403Forbidden,
            var code when code.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
                       || code.Contains("DoubleBooking", StringComparison.OrdinalIgnoreCase)
                       || code.Contains("Unavailable", StringComparison.OrdinalIgnoreCase)
                       || code.Contains("Violation", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitleForStatusCode(statusCode),
            Detail = error.Description,
            Instance = HttpContext.Request.Path,
            Extensions =
            {
                ["code"] = error.Code,
                ["correlationId"] = HttpContext.Items["X-Correlation-ID"]?.ToString() ?? HttpContext.TraceIdentifier
            }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "An Error Occurred"
    };
}
