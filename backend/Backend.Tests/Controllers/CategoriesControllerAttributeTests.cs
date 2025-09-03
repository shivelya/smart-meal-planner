using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Controllers
{
    public class CategoriesControllerAttributeTests
    {
        private readonly Type _controllerType = typeof(CategoriesController);

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
        public void GetCategories_HasHttpGetAttribute()
        {
            var method = _controllerType.GetMethod("GetCategories");
            Assert.NotNull(method);
            var attr = method.GetCustomAttribute<HttpGetAttribute>();
            Assert.NotNull(attr);
        }
    }
}
