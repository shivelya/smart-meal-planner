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
        [InlineData("CreateAsync", typeof(HttpPostAttribute))]
        [InlineData("GetByIdAsync", typeof(HttpGetAttribute))]
        [InlineData("GetByIdsAsync", typeof(HttpPostAttribute))]
        [InlineData("SearchAsync", typeof(HttpGetAttribute))]
        [InlineData("UpdateAsync", typeof(HttpPutAttribute))]
        [InlineData("DeleteAsync", typeof(HttpDeleteAttribute))]
        [InlineData("ExtractRecipeAsync", typeof(HttpPostAttribute))]
        [InlineData("CookRecipe", typeof(HttpPutAttribute))]
        public void Endpoint_HasCorrectHttpAttribute(string methodName, Type expectedAttribute)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method.GetCustomAttributes(expectedAttribute, false).FirstOrDefault();
            Assert.NotNull(attr);
        }

        [Theory]
        [InlineData("CreateAsync", StatusCodes.Status201Created)]
        [InlineData("CreateAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("GetByIdAsync", StatusCodes.Status404NotFound)]
        [InlineData("GetByIdAsync", StatusCodes.Status200OK)]
        [InlineData("GetByIdAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("GetByIdsAsync", StatusCodes.Status200OK)]
        [InlineData("GetByIdsAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("SearchAsync", StatusCodes.Status200OK)]
        [InlineData("SearchAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("UpdateAsync", StatusCodes.Status200OK)]
        [InlineData("UpdateAsync", StatusCodes.Status404NotFound)]
        [InlineData("UpdateAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("DeleteAsync", StatusCodes.Status204NoContent)]
        [InlineData("DeleteAsync", StatusCodes.Status404NotFound)]
        [InlineData("DeleteAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("ExtractRecipeAsync", StatusCodes.Status200OK)]
        [InlineData("ExtractRecipeAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("CookRecipe", StatusCodes.Status200OK)]
        [InlineData("CookRecipe", StatusCodes.Status400BadRequest)]
        [InlineData("CookRecipe", StatusCodes.Status500InternalServerError)]
        public void Endpoint_HasCorrectProducesResponseType(string methodName, int statusCode)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Contains(attrs, a => a.StatusCode == statusCode);
        }
    }
}