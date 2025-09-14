using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Tests.Controllers
{
    public class AuthControllerAttributeTests
    {
        [Fact]
        public void Refresh_HasAuthorizeAttribute()
        {
            var method = typeof(AuthController).GetMethod("RefreshAsync");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        }

        [Fact]
        public void Logout_HasAuthorizeAttribute()
        {
            var method = typeof(AuthController).GetMethod("Logout");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        }
        
        [Fact]
        public void Register_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("Register");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            var route = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
            Assert.Equal("register", route);
        }

        [Fact]
        public void Login_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("Login");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            var route = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
            Assert.Equal("login", route);
        }

        [Fact]
        public void Refresh_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("RefreshAsync");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            var route = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
            Assert.Equal("refresh", route);
        }

        [Fact]
        public void Logout_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("Logout");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            var route = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
            Assert.Equal("logout", route);
        }

        [Fact]
        public void Controller_HasApiControllerAndRouteAttribute()
        {
            var type = typeof(AuthController);
            Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
            var route = type.GetCustomAttribute<RouteAttribute>()?.Template;
            Assert.Equal("api/[controller]", route);
        }
    }
}
