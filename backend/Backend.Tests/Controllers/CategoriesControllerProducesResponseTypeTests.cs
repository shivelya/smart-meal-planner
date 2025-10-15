using System.Linq;
using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class CategoriesControllerProducesResponseTypeTests
    {
        private readonly Type _controllerType = typeof(CategoryController);

        [Fact]
        public void GetCategories_HasCorrectProducesResponseTypeAttributes()
        {
            var method = _controllerType.GetMethod("GetCategories");
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
            Assert.Equal(2, attrs.Count);
            Assert.Contains(attrs, a => a.StatusCode == 200);
            Assert.Contains(attrs, a => a.StatusCode == 500);
        }
    }
}
