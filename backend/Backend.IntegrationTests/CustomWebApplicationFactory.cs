using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Backend.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder().Build();

        public Task InitializeAsync()
        {
            return _databaseContainer.StartAsync();
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        public Task DisposeAsync()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            _databaseContainer.StopAsync();
            return _databaseContainer.DisposeAsync().AsTask();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddDbContext<PlannerContext>(options =>
                {
                    options.UseNpgsql(_databaseContainer.GetConnectionString());
                });
            });
        }
    }
}