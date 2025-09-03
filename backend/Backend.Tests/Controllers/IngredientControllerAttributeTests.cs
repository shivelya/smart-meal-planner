using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Controllers
{
    public class IngredientControllerAttributeTests
    {
        private readonly Type _controllerType = typeof(IngredientController);

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

        [Fact]
        public void SearchIngredients_HasHttpGetAttribute()
        {
            var method = _controllerType.GetMethod("SearchIngredients");
            Assert.NotNull(method);
            var attr = method.GetCustomAttribute<HttpGetAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void SearchIngredients_HasProducesResponseTypeAttribute_400BadRequest()
        {
            var method = _controllerType.GetMethod("SearchIngredients");
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Contains(attrs, a => a.StatusCode == StatusCodes.Status400BadRequest);
        }
    }
}
