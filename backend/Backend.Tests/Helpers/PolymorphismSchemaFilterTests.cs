using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Helpers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Moq;
using Xunit;

namespace Backend.Tests.Helpers
{
    public class PolymorphismSchemaFilterTests
    {
        [Fact]
        public void Apply_SetsDiscriminatorAndOneOf_WhenTypeIsBaseType()
        {
            // Arrange
            var subTypes = new[] { typeof(NewFoodReferenceDtoDummy), typeof(ExistingFoodReferenceDtoDummy) };
            var filter = new PolymorphismSchemaFilter<FoodReferenceDtoDummy>(subTypes);
            var schema = new OpenApiSchema();
            var context = new SchemaFilterContext(
                typeof(FoodReferenceDtoDummy),
                null,
                null,
                null
            );

            // Act
            filter.Apply(schema, context);

            // Assert
            Assert.NotNull(schema.Discriminator);
            Assert.Equal("mode", schema.Discriminator.PropertyName);
            Assert.Equal(2, schema.Discriminator.Mapping.Count);
            Assert.Contains("existing", schema.Discriminator.Mapping.Keys);
            Assert.Contains("new", schema.Discriminator.Mapping.Keys);
            Assert.NotNull(schema.OneOf);
            Assert.Equal(2, schema.OneOf.Count);
            Assert.Contains(schema.OneOf, s => s.Reference.Id == "NewFoodReferenceDtoDummy");
            Assert.Contains(schema.OneOf, s => s.Reference.Id == "ExistingFoodReferenceDtoDummy");
        }

        [Fact]
        public void Apply_DoesNothing_WhenTypeIsNotBaseType()
        {
            // Arrange
            var subTypes = new[] { typeof(NewFoodReferenceDtoDummy), typeof(ExistingFoodReferenceDtoDummy) };
            var filter = new PolymorphismSchemaFilter<FoodReferenceDtoDummy>(subTypes);
            var schema = new OpenApiSchema();
            var context = new SchemaFilterContext(
                typeof(NewFoodReferenceDtoDummy),
                null,
                null,
                null
            );

            // Act
            filter.Apply(schema, context);

            // Assert
            Assert.Null(schema.Discriminator);
            Assert.Empty(schema.OneOf);
        }
    }

    // Dummy DTOs for test context
    public class FoodReferenceDtoDummy { }
    public class NewFoodReferenceDtoDummy : FoodReferenceDtoDummy { }
    public class ExistingFoodReferenceDtoDummy : FoodReferenceDtoDummy { }
}
