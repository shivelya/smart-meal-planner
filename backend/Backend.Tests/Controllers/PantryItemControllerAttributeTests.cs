using System.Linq;
using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class PantryItemControllerAttributeTests
    {
        private readonly Type _controllerType = typeof(PantryItemController);

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
        [InlineData("AddItem", typeof(HttpPostAttribute))]
        [InlineData("AddItems", typeof(HttpPostAttribute))]
        [InlineData("GetItem", typeof(HttpGetAttribute))]
        // Add more endpoint checks as needed
        public void Endpoint_HasCorrectHttpAttribute(string methodName, Type expectedAttribute)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method.GetCustomAttributes(expectedAttribute, false).FirstOrDefault();
            Assert.NotNull(attr);
        }

        [Theory]
        [InlineData("AddItem", StatusCodes.Status200OK)]
        [InlineData("AddItem", StatusCodes.Status500InternalServerError)]
        [InlineData("AddItems", StatusCodes.Status201Created)]
        [InlineData("AddItems", StatusCodes.Status500InternalServerError)]
        [InlineData("GetItem", StatusCodes.Status200OK)]
        [InlineData("GetItem", StatusCodes.Status404NotFound)]
        [InlineData("GetItem", StatusCodes.Status500InternalServerError)]
        [InlineData("GetItems", StatusCodes.Status200OK)]
        [InlineData("GetItems", StatusCodes.Status500InternalServerError)]
        [InlineData("DeleteItem", StatusCodes.Status204NoContent)]
        [InlineData("DeleteItem", StatusCodes.Status404NotFound)]
        [InlineData("DeleteItem", StatusCodes.Status500InternalServerError)]
        [InlineData("DeleteItems", StatusCodes.Status204NoContent)]
        [InlineData("DeleteItems", StatusCodes.Status404NotFound)]
        [InlineData("DeleteItems", StatusCodes.Status500InternalServerError)]
        public void Endpoint_HasCorrectProducesResponseType(string methodName, int statusCode)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Contains(attrs, a => a.StatusCode == statusCode);
        }
    }
}
