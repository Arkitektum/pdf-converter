using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PdfGenerator.Configuration
{
    public class MultipartOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiKeyParameter = new OpenApiParameter
            {
                Name = "X-API-KEY",
                In = ParameterLocation.Header,
                Schema = new OpenApiSchema { Type = "string" },
                Required = true,
            };

            if (context.ApiDescription.RelativePath == "pdf/fromHtmlString")
            {
                operation.Parameters.Insert(0, apiKeyParameter);
            }

            if (context.ApiDescription.RelativePath == "pdf/fromImage")
            {
                operation.Parameters.Insert(0, apiKeyParameter);

                var mediaType = new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema()
                    {
                        Type = "object",
                        Properties =
                        {
                            ["image"] = new OpenApiSchema
                            {
                                Type = "file",
                                Format = "binary"
                            },
                            ["title"] = new OpenApiSchema
                            {
                                Type = "string"
                            }
                        },
                        Required = new HashSet<string>() { "image" }
                    }
                };
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = { ["multipart/form-data"] = mediaType }
                };
            }
        }
    }
}
