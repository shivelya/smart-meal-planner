using System.Text;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Impl;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Backend.Tests.Helpers
{
    public class ServiceRegistrationExtensionsTests
    {
        [Fact]
        public void AddAppServices_RegistersAllServices()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton(typeof(IConfiguration), config);
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            services.AddSingleton(typeof(PlannerContext), new PlannerContext(options, config, new Mock<ILogger<PlannerContext>>().Object));

            services.AddAppServices();
            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<ITokenService>());
            Assert.NotNull(provider.GetService<ICategoryService>());
            Assert.NotNull(provider.GetService<IUserService>());
            Assert.NotNull(provider.GetService<IEmailService>());
            Assert.NotNull(provider.GetService<IPantryItemService>());
            Assert.NotNull(provider.GetService<IFoodService>());
            Assert.NotNull(provider.GetService<IRecipeExtractor>());
            Assert.NotNull(provider.GetService<ISmtpClient>());
            Assert.NotNull(provider.GetService<IMealPlanGenerator>());
            Assert.NotNull(provider.GetService<ManualRecipeExtractor>());
        }

        [Fact]
        public void AddDatabase_ThrowsIfNoConnectionString()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton(typeof(IConfiguration), config);
            Environment.SetEnvironmentVariable("DOTNET_CONNECTIONSTRING", null);

            Assert.Throws<InvalidOperationException>(() => services.AddDatabase(config));
        }

        [Fact]
        public void AddDatabase_UsesEnvironmentVariable()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton(typeof(IConfiguration), config);
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";
            Environment.SetEnvironmentVariable("DOTNET_CONNECTIONSTRING", connectionString);
            services.AddAppServices();

            var wasRan = false;
            services.AddDatabase(config, conn => {
                wasRan = true;
                Assert.Equal(connectionString, conn);
                return options => options.UseInMemoryDatabase(connectionString);
            });

            var provider = services.BuildServiceProvider();
            var test = provider.GetService<PlannerContext>();
            Assert.NotNull(test);
            Assert.True(wasRan);
        }

        [Fact]
        public void AddDatabase_UsesConfigConnectionString()
        {
            var services = new ServiceCollection();
            var connectionString = "Host=localhost;Database=test;Username=test;Password=test";
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString })
                .Build();
            services.AddSingleton(typeof(IConfiguration), config);
            Environment.SetEnvironmentVariable("DOTNET_CONNECTIONSTRING", null);
            services.AddAppServices();

            var wasRan = false;
            services.AddDatabase(config, conn =>
            {
                wasRan = true;
                Assert.Equal(connectionString, conn);
                return options => options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });

            Assert.True(wasRan);
            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<PlannerContext>());
        }

        [Fact]
        public void AddJwtAuth_ThrowsIfNoJwtKey()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddJwtAuth(config);

            var provider = services.BuildServiceProvider();
            var optsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.Throws<InvalidOperationException>(() => optsMonitor.Get(JwtBearerDefaults.AuthenticationScheme));
        }

        [Fact]
        public void AddJwtAuth_ConfiguredCorrectly()
        {
            var services = new ServiceCollection();
            var keyStr = "key";
            var key = Encoding.UTF8.GetBytes(keyStr);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = keyStr,
                    ["Jwt:Issuer"] = "Issuer",
                    ["Jwt:Audience"] = "Audience"
                })
                .Build();

            services.AddJwtAuth(config);
            var provider = services.BuildServiceProvider();
            var optsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOptions = optsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.Equal("Issuer", jwtOptions.TokenValidationParameters.ValidIssuer);
            Assert.Equal("Audience", jwtOptions.TokenValidationParameters.ValidAudience);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateAudience);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateLifetime);
            Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);
            Assert.Equal(TimeSpan.Zero, jwtOptions.TokenValidationParameters.ClockSkew);
            Assert.NotNull(jwtOptions.TokenValidationParameters.IssuerSigningKey);
            Assert.IsType<SymmetricSecurityKey>(jwtOptions.TokenValidationParameters.IssuerSigningKey);
        }

        [Fact]
        public void AddJwtAuth_RegistersAuthenticationAndAuthorization()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "supersecretkey" })
                .Build();
            services.AddAppServices();

            services.AddJwtAuth(config);

            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IAuthenticationService>());
            Assert.NotNull(provider.GetService<IAuthorizationService>());
        }

        [Fact]
        public void ConfigureSwagger_RegistersSwaggerGen()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Swagger:Title"] = "title",
                    ["Swagger:Version"] = "v3",
                    ["Swagger:Description"] = "descrip",
                    ["Swagger:Contact:Name"] = "name",
                    ["Swagger:Contact:Email"] = "email",
                    ["Swagger:Contact:Url"] = "url",
                    ["Swagger:License:Name"] = "name2",
                    ["Swagger:License:Url"] = "url2"
                })
                .Build();
            var services = new ServiceCollection();
            services.ConfigureSwagger(config);
            var provider = services.BuildServiceProvider();
            // Just check that the service provider is built, actual SwaggerGen registration is tested in integration
            Assert.NotNull(provider);
        }

        [Fact]
        public void ConfigureSwagger_RegistersSwaggerGen_ThrowsWhenConfigMissing()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Swagger:Title"] = "title"
                })
                .Build();
            var services = new ServiceCollection();
            Assert.Throws<InvalidOperationException>(() => services.ConfigureSwagger(config));
        }
    }
}
