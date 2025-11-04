using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace TodoList.API.Extensions
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Handle IFormFile and file uploads so Swashbuckle generates a multipart/form-data requestBody
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile)
                            || p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFile[])
                            || p.ParameterType == typeof(Microsoft.AspNetCore.Http.IFormFileCollection))
                .ToList();

            if (!fileParameters.Any())
                return;

            // Ensure operation.Parameters exists and remove file parameters from it (they will be moved to requestBody)
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var names = fileParameters.Select(p => p.Name).ToHashSet(System.StringComparer.OrdinalIgnoreCase);
                operation.Parameters = operation.Parameters.Where(p => !names.Contains(p.Name)).ToList();
            }

            var properties = new Dictionary<string, OpenApiSchema>();
            foreach (var p in fileParameters)
            {
                var propName = p.Name ?? p.ParameterType.Name.ToLowerInvariant();
                properties.Add(propName, new OpenApiSchema { Type = "string", Format = "binary" });
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties
                        }
                    }
                }
            };
        }
    }
}