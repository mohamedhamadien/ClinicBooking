using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClinicBooking.API.Swagger;

public sealed class SwaggerHeadersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Slug",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Multi-tenant clinic identifier (e.g., 'apex-health')",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Preferred language for localized responses ('en-US' or 'ar-EG')",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}
