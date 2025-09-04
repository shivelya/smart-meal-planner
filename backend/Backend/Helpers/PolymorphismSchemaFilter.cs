using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Backend.Helpers
{
    public class PolymorphismSchemaFilter<TBase>(IEnumerable<Type> subTypes) : ISchemaFilter
    {
        private readonly IEnumerable<Type> _subTypes = subTypes;

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(TBase))
            {
                schema.Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "mode",
                    Mapping = new Dictionary<string, string>
                    {
                        { "existing", "#/components/schemas/ExistingFoodReferenceDto" },
                        { "new", "#/components/schemas/NewFoodReferenceDto" }
                    }
                };
                schema.OneOf = [.. _subTypes
                    .Select(t => new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = t.Name
                        }
                    })];
            }
        }
    }
}
