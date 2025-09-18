using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests
{
    public class ErrorHandlingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ErrorHandlingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task InternalServerError_ShouldNotExposeSensitiveInfo()
        {
            var client = _factory.CreateClient();
            var loginResponse = await client.PostAsJsonAsync("/api/auth/register", new LoginRequest
            {
                Email = "test@example.com",
                Password = "testpass"
            });

            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            // Hit an endpoint that will fail (e.g. invalid ID)
            var response = await client.PutAsJsonAsync("api/Auth/update-user", new { });

            // this returns 404, I'll have to think of something I can do for for it to fail better.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("error", body.ToLower());
            Assert.DoesNotContain("System.", body);   // No stack trace
            Assert.DoesNotContain("SqlException", body);
        }
    }

    public class ConfigurationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ConfigurationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Environment_ShouldBeDevelopmentByDefault()
        {
            var hostEnv = _factory.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.Equal("Development", hostEnv.EnvironmentName);
        }

        [Fact]
        public void DifferentEnvironments_ShouldUseDifferentConnectionStrings()
        {
            var config = _factory.Services.GetService(typeof(IConfiguration)) as IConfiguration;

            var devConn = config?.GetConnectionString("DefaultConnection");
            // simulate production override
            var prodConn = config?.GetSection("ConnectionStrings:ProdConnection")?.Value;

            Assert.NotEqual(devConn, prodConn);
        }
    }

    public class LoggingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public LoggingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task FailedRequest_ShouldWriteLogEntry()
        {
            var output = new StringWriter();
            Console.SetOut(output);

            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/bad-endpoint");
            output.Flush();

            string consoleOutput = output.ToString();
            Assert.Contains("Warning", consoleOutput);
        }
    }
}