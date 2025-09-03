using System.Linq;
using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class CategoriesControllerProducesResponseTypeTests
    {
        private readonly Type _controllerType = typeof(CategoriesController);

        [Fact]
        public void GetCategories_HasNoProducesResponseTypeAttributes()
        {
            var method = _controllerType.GetMethod("GetCategories");
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
            Assert.Empty(attrs); // No ProducesResponseType attributes present
        }
    }
}
