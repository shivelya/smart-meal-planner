using Backend.DTOs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Helpers
{
    public class EnsureSubTypesDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var subTypes = new[]
            {
            typeof(ExistingIngredientDto),
            typeof(NewIngredientDto),
        };

            foreach (var type in subTypes)
            {
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
            }
        }
    }
}