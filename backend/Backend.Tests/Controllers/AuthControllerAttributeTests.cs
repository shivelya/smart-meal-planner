using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Tests.Controllers
{
    public class AuthControllerAttributeTests
    {
        public static IEnumerable<object[]> ProducesResponseCases()
        {
            yield return new object[] { ("RegisterAsync", new[] { 200, 400, 500 }) };
            yield return new object[] { ("LoginAsync", new[] { 200, 400, 401, 500 }) };
            yield return new object[] { ("RefreshAsync", new[] { 200, 400, 401, 500 }) };
            yield return new object[] { ("LogoutAsync", new[] { 200, 400 }) };
            yield return new object[] { ("UpdateUserAsync", new[] { 200, 400, 500 }) };
            yield return new object[] { ("ChangePasswordAsync", new[] { 200, 400, 401, 500 }) };
            yield return new object[] { ("ForgotPassword", new[] { 200, 500 }) };
            yield return new object[] { ("ResetPassword", new[] { 200, 400, 500 }) };
        }

        [Theory]
        [MemberData(nameof(ProducesResponseCases))]
        public void Method_HasExpectedProducesResponseTypes((string MethodName, int[] StatusCodes) data)
        {
            var method = typeof(AuthController).GetMethod(data.MethodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false).Cast<ProducesResponseTypeAttribute>().ToList();
            var actualCodes = attrs.Select(a => a.StatusCode).OrderBy(x => x).ToArray();
            var expectedCodes = data.StatusCodes.OrderBy(x => x).ToArray();
            Assert.Equal(expectedCodes, actualCodes);
        }
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
            var method = typeof(AuthController).GetMethod("LogoutAsync");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        }
        
        [Fact]
        public void Register_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("RegisterAsync");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            var route = method.GetCustomAttribute<HttpPostAttribute>()?.Template;
            Assert.Equal("register", route);
        }

        [Fact]
        public void Login_HasHttpPostAndRouteAttribute()
        {
            var method = typeof(AuthController).GetMethod("LoginAsync");
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
            var method = typeof(AuthController).GetMethod("LogoutAsync");
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
