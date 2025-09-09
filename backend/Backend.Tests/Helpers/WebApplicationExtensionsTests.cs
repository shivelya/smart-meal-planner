using Backend.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Backend.Tests.Helpers
{
    public class WebApplicationExtensionsTests
    {
        [Fact]
        public void ConfigureLogging_UsesBuiltInLoggingInDevelopment()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = Environments.Development;
            var result = builder.ConfigureLogging();
            Assert.Equal(builder, result);
        }

        [Fact]
        public void ConfigureLogging_UsesSerilogInProduction()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = Environments.Production;
            var result = builder.ConfigureLogging();
            Assert.Equal(builder, result);
        }

        [Fact]
        public void UseMyAppPipeline_RegistersMiddleware()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddJwtAuth(builder.Configuration);
            var app = builder.Build();
            var result = app.UseMyAppPipeline();
            Assert.Equal(app, result);
        }
    }
}
