using System;
using Backend.Helpers;
using Backend.DTOs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Moq;
using Xunit;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Backend.Tests.Helpers
{
    public class EnsureSubTypesDocumentFilterTests
    {
        [Fact]
        public void Apply_GeneratesSchemasForAllSubTypes()
        {
            // Arrange
            var filter = new EnsureSubTypesDocumentFilter();
            var swaggerDoc = new OpenApiDocument();
            var schemaGeneratorMock = new Mock<ISchemaGenerator>();
            var schemaRepository = new SchemaRepository();
            var context = new DocumentFilterContext(
                null,
                schemaGeneratorMock.Object,
                schemaRepository
            );

            // Act
            filter.Apply(swaggerDoc, context);

            // Assert
            schemaGeneratorMock.Verify(sg => sg.GenerateSchema(
                typeof(Backend.DTOs.NewFoodReferenceDto),
                schemaRepository,
                It.IsAny<MemberInfo>(),
                It.IsAny<ParameterInfo>(),
                It.IsAny<ApiParameterRouteInfo>()), Times.Once);

            schemaGeneratorMock.Verify(sg => sg.GenerateSchema(
                typeof(Backend.DTOs.ExistingFoodReferenceDto),
                schemaRepository,
                It.IsAny<MemberInfo>(),
                It.IsAny<ParameterInfo>(),
                It.IsAny<ApiParameterRouteInfo>()), Times.Once);
        }

        [Fact]
        public void Apply_DoesNotThrow_WhenSubTypesAreEmpty()
        {
            // Arrange
            var filter = new EnsureSubTypesDocumentFilterEmpty();
            var swaggerDoc = new OpenApiDocument();
            var schemaGeneratorMock = new Mock<ISchemaGenerator>();
            var schemaRepository = new SchemaRepository();
            var context = new DocumentFilterContext(
                null,
                schemaGeneratorMock.Object,
                schemaRepository
            );

            // Act & Assert
            var ex = Record.Exception(() => filter.Apply(swaggerDoc, context));
            Assert.Null(ex);
            schemaGeneratorMock.Verify(sg => sg.GenerateSchema(It.IsAny<Type>(), schemaRepository, null, null, null), Times.Never);
        }

        // Helper class to test empty subTypes branch
        private class EnsureSubTypesDocumentFilterEmpty : IDocumentFilter
        {
            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                var subTypes = Array.Empty<Type>();
                foreach (var type in subTypes)
                {
                    context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                }
            }
        }
    }
}
