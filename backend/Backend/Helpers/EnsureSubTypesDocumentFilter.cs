using Backend.DTOs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class EnsureSubTypesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var subTypes = new[]
        {
            typeof(PantryItemRequestDto),
            typeof(CreateRecipeDtoRequest),
        };

        foreach (var type in subTypes)
        {
            context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}