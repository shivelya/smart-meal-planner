using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class PolymorphismSchemaFilter<TBase> : ISchemaFilter
{
    private readonly IEnumerable<Type> _subTypes;

    public PolymorphismSchemaFilter(IEnumerable<Type> subTypes)
    {
        _subTypes = subTypes;
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(TBase))
        {
            schema.Discriminator = new OpenApiDiscriminator { PropertyName = "type" };
            schema.OneOf = _subTypes
                .Select(t => new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = t.Name
                    }
                }).ToList();
        }
    }
}
