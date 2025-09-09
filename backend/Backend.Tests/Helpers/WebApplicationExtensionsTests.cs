using System.Collections;
using System.Reflection;
using Backend.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Helpers
{
    public class WebApplicationExtensionsTests
    {
        [Fact]
        public void ConfigureLogging_UsesBuiltInLoggingInDevelopment()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = Environments.Development;

            builder.ConfigureLogging();

            var host = builder.Build();
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            dynamic? providers = loggerFactory
                .GetType()
                .GetField("_providerRegistrations", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(loggerFactory);

            var console = false;
            var debug = false;
            Assert.NotNull(providers);
            foreach (var provider in providers!)
            {
                var innerProvider = provider.GetType().GetField("Provider").GetValue(provider);
                var name = innerProvider.GetType().Name;
                if (name.Contains("Console"))
                {
                    console = true;
                    var formatters = innerProvider.GetType().GetField("_formatters", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(innerProvider);
                    Assert.True(formatters.Keys.Contains("json"));
                    continue;
                }

                if (name.Contains("Debug"))
                {
                    debug = true;
                    continue;
                }
            }

            Assert.True(console);
            Assert.True(debug);
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
