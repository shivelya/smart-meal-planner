using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
        [InlineData("AddItemAsync", typeof(HttpPostAttribute))]
        [InlineData("AddItemsAsync", typeof(HttpPostAttribute), "bulk")]
        [InlineData("GetItemAsync", typeof(HttpGetAttribute), "{id:int}")]
        [InlineData("GetItemsAsync", typeof(HttpGetAttribute))]
        [InlineData("DeleteItemAsync", typeof(HttpDeleteAttribute), "{id:int}")]
        [InlineData("DeleteItemsAsync", typeof(HttpPostAttribute), "bulk-delete")]
        [InlineData("UpdateAsync", typeof(HttpPutAttribute), "{id:int}")]
        public void Endpoint_HasCorrectHttpAttribute(string methodName, Type expectedAttribute, string? template = null)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method.GetCustomAttributes(expectedAttribute, false).FirstOrDefault();
            Assert.NotNull(attr);

            HttpMethodAttribute httpAttr = (HttpMethodAttribute)attr!;
            Assert.NotNull(httpAttr);
            Assert.Equal(template, httpAttr.Template);
        }

        [Theory]
        [InlineData("AddItemAsync", StatusCodes.Status201Created)]
        [InlineData("AddItemAsync", StatusCodes.Status400BadRequest)]
        [InlineData("AddItemAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("AddItemsAsync", StatusCodes.Status201Created)]
        [InlineData("AddItemsAsync", StatusCodes.Status400BadRequest)]
        [InlineData("AddItemsAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("GetItemAsync", StatusCodes.Status200OK)]
        [InlineData("GetItemAsync", StatusCodes.Status404NotFound)]
        [InlineData("GetItemAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("GetItemsAsync", StatusCodes.Status200OK)]
        [InlineData("GetItemsAsync", StatusCodes.Status400BadRequest)]
        [InlineData("GetItemsAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("DeleteItemAsync", StatusCodes.Status204NoContent)]
        [InlineData("DeleteItemAsync", StatusCodes.Status404NotFound)]
        [InlineData("DeleteItemAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("DeleteItemsAsync", StatusCodes.Status204NoContent)]
        [InlineData("DeleteItemsAsync", StatusCodes.Status500InternalServerError)]
        [InlineData("UpdateItemAsync", StatusCodes.Status200OK)]
        [InlineData("UpdateItemAsync", StatusCodes.Status400BadRequest)]
        [InlineData("UpdateItemAsync", StatusCodes.Status500InternalServerError)]
        public void Endpoint_HasCorrectProducesResponseType(string methodName, int statusCode)
        {
            var method = _controllerType.GetMethod(methodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Contains(attrs, a => a.StatusCode == statusCode);
        }
    }
}
