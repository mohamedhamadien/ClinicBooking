using System.Security.Claims;
using System.Text.Json;
using ClinicBooking.API.Services;
using ClinicBooking.Application.Contracts.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string TenantHeader = "X-Tenant-Slug";
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IMutableTenantContext tenantContext,
        ITenantRepository tenantRepository)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip tenant resolution for static files, swagger, and health checks
        if (path.StartsWith("/swagger") || path.StartsWith("/health") || path.StartsWith("/api/v1/auth/login") || path.StartsWith("/api/v1/tenants"))
        {
            tenantContext.SetHost();
            await _next(context);
            return;
        }

        string? tenantSlug = null;

        // 1. Try header
        if (context.Request.Headers.TryGetValue(TenantHeader, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
        {
            tenantSlug = headerValue.ToString().Trim().ToLowerInvariant();
        }
        // 2. Try query parameter ?tenant=...
        else if (context.Request.Query.TryGetValue("tenant", out var queryValue) && !string.IsNullOrWhiteSpace(queryValue))
        {
            tenantSlug = queryValue.ToString().Trim().ToLowerInvariant();
        }
        // 3. Try JWT claims
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            tenantSlug = context.User.FindFirst("tenant")?.Value;
        }

        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            var tenant = await tenantRepository.GetByIdentifierAsync(tenantSlug, context.RequestAborted);
            if (tenant is null || !tenant.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Tenant Not Found",
                    Detail = $"Tenant '{tenantSlug}' was not found or is currently inactive."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                return;
            }

            tenantContext.SetTenant(tenant.Id, tenant.Identifier);
        }
        else
        {
            // If user has role Admin/ClinicAdmin or request is for global features, allow Host context
            if (context.User.IsInRole("Admin") || context.User.IsInRole("ClinicAdmin") || path.StartsWith("/api/v1/specialties"))
            {
                tenantContext.SetHost();
            }
            else
            {
                // Unauthenticated or patient/doctor request without tenant header
                tenantContext.SetHost();
            }
        }

        await _next(context);
    }
}
