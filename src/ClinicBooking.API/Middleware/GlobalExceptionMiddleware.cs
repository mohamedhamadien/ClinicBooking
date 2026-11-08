using System.Text.Json;
using ClinicBooking.Application.Common.Exceptions;
using ClinicBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdHeader]?.ToString()
            ?? context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        switch (exception)
        {
            case ValidationException valEx:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Detail = valEx.Message;
                problemDetails.Extensions["errors"] = valEx.Errors.Select(e => new { e.Code, e.Description });
                _logger.LogWarning(valEx, "Validation failure at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case DomainValidationException domainValEx:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Domain Rule Violation";
                problemDetails.Detail = domainValEx.Message;
                _logger.LogWarning(domainValEx, "Domain rule violation at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case PastAppointmentException pastEx:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Invalid Appointment Date";
                problemDetails.Detail = pastEx.Message;
                _logger.LogWarning(pastEx, "Past appointment attempt at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case ConcurrencyConflictException conflictEx:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Concurrency Conflict";
                problemDetails.Detail = conflictEx.Message;
                problemDetails.Extensions["code"] = conflictEx.Code;
                _logger.LogWarning(conflictEx, "Concurrency conflict at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case AppointmentCancellationPolicyException cancelEx:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Cancellation Policy Violation";
                problemDetails.Detail = cancelEx.Message;
                _logger.LogWarning(cancelEx, "Cancellation policy violation at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case DoctorUnavailableException docUnavailEx:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Doctor Unavailable";
                problemDetails.Detail = docUnavailEx.Message;
                _logger.LogWarning(docUnavailEx, "Doctor unavailable conflict at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case InvalidAppointmentStateException stateEx:
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Invalid Status Transition";
                problemDetails.Detail = stateEx.Message;
                _logger.LogWarning(stateEx, "Invalid appointment state transition at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            case UnauthorizedAccessException unauthEx:
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = unauthEx.Message;
                _logger.LogWarning(unauthEx, "Forbidden access at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;

            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "An Unexpected Error Occurred";
                problemDetails.Detail = "A server error occurred while processing your request. Please reference the correlation ID when contacting support.";
                _logger.LogError(exception, "Unhandled exception at {Path} (Correlation: {CorrelationId})", context.Request.Path, correlationId);
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status.Value;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
