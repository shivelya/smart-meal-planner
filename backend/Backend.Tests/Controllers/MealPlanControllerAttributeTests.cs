using System.Reflection;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Controllers
{
    public class MealPlanControllerAttributeTests
    {
        [Fact]
        public void Controller_HasApiControllerAttribute()
        {
            var attr = typeof(MealPlanController).GetCustomAttribute<ApiControllerAttribute>();
            Assert.NotNull(attr);
        }

        [Fact]
        public void Controller_HasRouteAttribute()
        {
            var attr = typeof(MealPlanController).GetCustomAttribute<RouteAttribute>();
            Assert.NotNull(attr);
            Assert.Equal("api/meal-plan", attr.Template);
        }

        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            var attr = typeof(MealPlanController).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
        }

        [Theory]
        [InlineData("GetMealPlansAsync", typeof(HttpGetAttribute))]
        [InlineData("AddMealPlanAsync", typeof(HttpPostAttribute))]
        [InlineData("UpdateMealPlanAsync", typeof(HttpPutAttribute))]
        [InlineData("DeleteMealPlanAsync", typeof(HttpDeleteAttribute))]
        [InlineData("GenerateMealPlanAsync", typeof(HttpPostAttribute))]
        public void Methods_HaveHttpVerbAttribute(string methodName, Type attributeType)
        {
            var method = typeof(MealPlanController).GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method.GetCustomAttributes(attributeType, false).FirstOrDefault();
            Assert.NotNull(attr);
        }

        [Theory]
        [InlineData("GetMealPlansAsync", 200)]
        [InlineData("GetMealPlansAsync", 500)]
        [InlineData("AddMealPlanAsync", 201)]
        [InlineData("AddMealPlanAsync", 400)]
        [InlineData("AddMealPlanAsync", 500)]
        [InlineData("UpdateMealPlanAsync", 200)]
        [InlineData("UpdateMealPlanAsync", 400)]
        [InlineData("UpdateMealPlanAsync", 500)]
        [InlineData("DeleteMealPlanAsync", 204)]
        [InlineData("DeleteMealPlanAsync", 404)]
        [InlineData("DeleteMealPlanAsync", 500)]
        [InlineData("GenerateMealPlanAsync", 200)]
        [InlineData("GenerateMealPlanAsync", 400)]
        [InlineData("GenerateMealPlanAsync", 500)]
        public void Methods_HaveProducesResponseTypeAttribute(string methodName, int statusCode)
        {
            var method = typeof(MealPlanController).GetMethod(methodName);
            Assert.NotNull(method);
            var attrs = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
                .Cast<ProducesResponseTypeAttribute>()
                .Select(a => a.StatusCode);
            Assert.Contains(statusCode, attrs);
        }
    }
}
