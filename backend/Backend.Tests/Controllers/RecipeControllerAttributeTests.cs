using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Controllers
{
    public class RecipeControllerAttributeTests
    {
        private readonly Type _controllerType = typeof(RecipeController);

        [Fact]
        public void Controller_HasApiControllerAttribute()
        {
            var attr = _controllerType.GetCustomAttribute<ApiControllerAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            var attr = _controllerType.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void Controller_HasRouteAttribute()
        {
            var attr = _controllerType.GetCustomAttribute<RouteAttribute>();
            Assert.NotNull(attr);
            Assert.Equal("api/[controller]", attr.Template);
        }

        [Theory]
        [InlineData("Create", typeof(HttpPostAttribute))]
        [InlineData("GetById", typeof(HttpGetAttribute))]
        [InlineData("GetByIds", typeof(HttpPostAttribute))]
        [InlineData("Search", typeof(HttpGetAttribute))]
        [InlineData("Update", typeof(HttpPutAttribute))]
        [InlineData("Delete", typeof(HttpDeleteAttribute))]
        [InlineData("ExtractRecipe", typeof(HttpPostAttribute))]
        public void Endpoint_HasCorrectHttpAttribute(string methodName, Type expectedAttribute)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method.GetCustomAttributes(expectedAttribute, false).FirstOrDefault();
            Assert.NotNull(attr);
        }

        [Theory]
        [InlineData("Create", StatusCodes.Status201Created)]
        [InlineData("Create", StatusCodes.Status500InternalServerError)]
        [InlineData("GetById", StatusCodes.Status404NotFound)]
        [InlineData("GetById", StatusCodes.Status200OK)]
        [InlineData("GetById", StatusCodes.Status500InternalServerError)]
        [InlineData("GetByIds", StatusCodes.Status200OK)]
        [InlineData("GetByIds", StatusCodes.Status500InternalServerError)]
        [InlineData("Search", StatusCodes.Status200OK)]
        [InlineData("Search", StatusCodes.Status500InternalServerError)]
        [InlineData("Update", StatusCodes.Status200OK)]
        [InlineData("Update", StatusCodes.Status404NotFound)]
        [InlineData("Update", StatusCodes.Status500InternalServerError)]
        [InlineData("Delete", StatusCodes.Status204NoContent)]
        [InlineData("Delete", StatusCodes.Status404NotFound)]
        [InlineData("Delete", StatusCodes.Status500InternalServerError)]
        [InlineData("ExtractRecipe", StatusCodes.Status200OK)]
        [InlineData("ExtractRecipe", StatusCodes.Status500InternalServerError)]
        public void Endpoint_HasCorrectProducesResponseType(string methodName, int statusCode)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Contains(attrs, a => a.StatusCode == statusCode);
        }
    }
}