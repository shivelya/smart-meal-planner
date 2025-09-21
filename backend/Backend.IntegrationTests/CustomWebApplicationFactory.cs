using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Backend.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder()
            .WithDatabase(Guid.NewGuid().ToString())
            .Build();

        public Dictionary<string, string>? ConfigValues { get; set; }

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
                // if ConfigValues has a value then override configuration
                if (ConfigValues != null)
                    OverwriteConfig(services);

                // swap email service
                services.RemoveAll<IEmailService>();
                services.AddSingleton<FakeEmailService>();
                services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailService>());

                // swap db context
                services.AddDbContext<PlannerContext>(options =>
                {
                    options.UseNpgsql(_databaseContainer.GetConnectionString());
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PlannerContext>();
                db.Database.EnsureCreated();

                // seed data
                db.Users.Add(new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") });

                db.Foods.Add(new Food { CategoryId = 1, Name = "Bananas" });
                db.Foods.Add(new Food { CategoryId = 1, Name = "Spaghetti" });
                db.Foods.Add(new Food { CategoryId = 2, Name = "Fried Chicken" });
                db.Foods.Add(new Food { CategoryId = 3, Name = "Dumplings" });

                db.SaveChanges();

                db.Recipes.Add(new Recipe
                {
                    Source = "Spoonacular - 636603",
                    UserId = 1,
                    Title = "Butternut Squash Soup with Fresh Goat Cheese",
                    Instructions = "Peel and remove seeds from the squash cut in large 2 inch pieces.\nPlace squash in heavy gauge sauce pot and cover with the milk and water.\nBring to a boil and simmer for 20 minutes until very tender.\nAdd 1 package Chavrie Goat Cheese Pyramid and bring back to a simmer.\nRemove from the heat.\nCarefully remove squash with a slotted spoon and place in blender. Add enough of the cooking liquid to cover the squash puree in the blender. Be very careful: you must leave the center cap of the blender off so you do not trap the steam (cover loosely with a towel).\nPour into a 1 gallon sauce pot and keep warm.\nReserve and repeat until all the squash has been pureed.\nAdjust to desired consistency with the remaining cooking liquid. Season with salt and pepper.\nTo serve, ladle the hot soup into the individual soup bowls and top with a dollop of Chavrie Goat Cheese Pyramid garnish with fresh herbs.",
                    Ingredients = [
                        new RecipeIngredient { Food = new Food { CategoryId = 1, Name = "Goat Cheere"}, Quantity = 10.6M, Unit = "oz" },
                            new RecipeIngredient { Food = new Food { CategoryId = 1, Name = "Butternut Squash"}, Quantity = 1 },
                            new RecipeIngredient { Food = new Food { CategoryId = 1, Name = "water" }, Quantity = 0.5M, Unit = "qt" },
                            new RecipeIngredient { Food = new Food { CategoryId = 1, Name = "Milk"}, Quantity = 0.5M, Unit = "qt" }
                    ]
                });

                db.PantryItems.Add(new PantryItem { FoodId = 1, UserId = 1, Quantity = 3, Unit = "bunches" });
                db.PantryItems.Add(new PantryItem { FoodId = 3, UserId = 1, Quantity = 1, Unit = "bag" });
                db.PantryItems.Add(new PantryItem { FoodId = 6, Quantity = 12, UserId = 1 });

                db.MealPlans.Add(new MealPlan
                {
                    StartDate = DateTime.UtcNow.AddDays(-2),
                    UserId = 1,
                    Meals =
                    [
                        new MealPlanEntry
                        {
                            Cooked = false,
                            Notes = "I am a note",
                            RecipeId = 1
                        },
                        new MealPlanEntry
                        {
                            Notes = "eat out tonight we are busy",
                        }
                    ]
                });
                db.MealPlans.Add(new MealPlan
                {
                    StartDate = DateTime.UtcNow,
                    UserId = 1,
                    Meals =
                    [
                        new MealPlanEntry
                        {
                            Cooked = true,
                            Notes = "nachos"
                        }
                    ]
                });

                db.SaveChanges();
            });
        }

        private void OverwriteConfig(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var hostingEnvi = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{hostingEnvi.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(optional: true)
                .AddInMemoryCollection(ConfigValues!)
                .Build();

            services.AddSingleton<IConfiguration>(config);
        }

        public async Task<TokenResponse> LoginAsync(HttpClient client)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = "test@example.com",
                Password = "password"
            });

            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
            return result;
        }
    }
}