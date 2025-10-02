using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class MaintenanceTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient client;

        public MaintenanceTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            client = _factory.CreateClient();
            factory.LoginAsync(client).Wait();
        }

        [Fact]
        public async Task InternalServerError_ShouldNotExposeSensitiveInfo()
        {
            // Hit an endpoint that will fail (e.g. invalid ID)
            var response = await client.PutAsJsonAsync("api/Auth/update-user", new { });

            // this returns 404, I'll have to think of something I can do for for it to fail better.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("error", body.ToLower());
            Assert.DoesNotContain("System.", body);   // No stack trace
            Assert.DoesNotContain("SqlException", body);
        }

        [Fact]
        public void Environment_ShouldBeDevelopmentByDefault()
        {
            var hostEnv = _factory.Services.GetRequiredService<IWebHostEnvironment>();

            Assert.True(hostEnv.EnvironmentName == "Development" || hostEnv.EnvironmentName == "Integration");
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

        // I might struggle with this one unless I start logging to a file
        // [Fact]
        // public async Task FailedRequest_ShouldWriteLogEntry()
        // {
        //     var output = new StringWriter();
        //     Console.SetOut(output);

        //     var response = await client.GetAsync("/api/bad-endpoint");
        //     output.Flush();

        //     string consoleOutput = output.ToString();
        //     Assert.Contains("Warning", consoleOutput);
        // }
    }
}