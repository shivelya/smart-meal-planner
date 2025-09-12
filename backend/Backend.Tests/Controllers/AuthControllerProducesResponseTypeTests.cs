using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Controllers
{
    public class AuthControllerProducesResponseTypeTests
    {
        private readonly Type _controllerType = typeof(AuthController);

        [Fact]
        public void UpdateUserAsync_HasCorrectProducesResponseTypeAttributes()
        {
            var method = _controllerType.GetMethod("UpdateUserAsync");
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
            Assert.Equal(3, attrs.Count);
            Assert.Contains(attrs, a => a.StatusCode == 200);
            Assert.Contains(attrs, a => a.StatusCode == 400);
            Assert.Contains(attrs, a => a.StatusCode == 500);
        }
    }
}
