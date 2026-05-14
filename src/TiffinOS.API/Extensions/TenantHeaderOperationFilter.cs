using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TiffinOS.API.Extensions;

public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Slug",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Tenant slug — e.g. spicekitchen",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}