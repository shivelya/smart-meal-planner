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
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<CustomWebApplicationFactory>
    {
        // This class is just a marker; it doesn’t contain any code
    }

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
                OverwriteConfig(services);

                // swap email service
                services.RemoveAll<IEmailService>();
                services.AddSingleton<FakeEmailService>();
                services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<FakeEmailService>());

                // swap db context
                services.RemoveAll<DbContextOptions<PlannerContext>>();
                services.RemoveAll<PlannerContext>();
                services.AddDbContext<PlannerContext>(options =>
                {
                    options.UseNpgsql(_databaseContainer.GetConnectionString());
                });

                using var scope = services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PlannerContext>();

                context.Database.EnsureCreated();
            });
        }

        private void OverwriteConfig(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var hostingEnvi = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{hostingEnvi.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(optional: true);

            if (ConfigValues != null)
                configBuilder.AddInMemoryCollection(ConfigValues!);

            var config = configBuilder.Build();

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

        public void ResetDatabase()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PlannerContext>();

            context.MealPlans.RemoveRange(context.MealPlans);
            context.Recipes.RemoveRange(context.Recipes);
            context.ShoppingListItems.RemoveRange(context.ShoppingListItems);
            context.PantryItems.RemoveRange(context.PantryItems);
            context.SaveChanges();

            context.Foods.RemoveRange(context.Foods);
            context.Users.RemoveRange(context.Users);

            context.RefreshTokens.RemoveRange(context.RefreshTokens);

            context.SaveChanges();

            SeedDatabase(context);
        }

        private static void SeedDatabase(PlannerContext context)
        {
            var user = new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
            context.Users.Add(user);

            var food1 = new Food { CategoryId = 1, Name = "Bananas" };
            var food2 = new Food { CategoryId = 1, Name = "Spaghetti" };
            var food3 = new Food { CategoryId = 2, Name = "Fried Chicken" };
            var food4 = new Food { CategoryId = 3, Name = "Dumplings" };
            context.Foods.Add(food1);
            context.Foods.Add(food2);
            context.Foods.Add(food3);
            context.Foods.Add(food4);

            context.SaveChanges();

            var food5 = new Food { CategoryId = 1, Name = "Goat Cheere" };
            var food6 = new Food { CategoryId = 1, Name = "Butternut Squash" };
            var food7 = new Food { CategoryId = 1, Name = "water" };
            var food8 = new Food { CategoryId = 1, Name = "Milk" };
            var recipe = new Recipe
            {
                Source = "Spoonacular - 636603",
                UserId = user.Id,
                Title = "Butternut Squash Soup with Fresh Goat Cheese",
                Instructions = "Peel and remove seeds from the squash cut in large 2 inch pieces.\nPlace squash in heavy gauge sauce pot and cover with the milk and water.\nBring to a boil and simmer for 20 minutes until very tender.\nAdd 1 package Chavrie Goat Cheese Pyramid and bring back to a simmer.\nRemove from the heat.\nCarefully remove squash with a slotted spoon and place in blender. Add enough of the cooking liquid to cover the squash puree in the blender. Be very careful: you must leave the center cap of the blender off so you do not trap the steam (cover loosely with a towel).\nPour into a 1 gallon sauce pot and keep warm.\nReserve and repeat until all the squash has been pureed.\nAdjust to desired consistency with the remaining cooking liquid. Season with salt and pepper.\nTo serve, ladle the hot soup into the individual soup bowls and top with a dollop of Chavrie Goat Cheese Pyramid garnish with fresh herbs.",
                Ingredients = [
                    new RecipeIngredient { Food = food5, Quantity = 10.6M, Unit = "oz" },
                        new RecipeIngredient { Food = food6, Quantity = 1 },
                        new RecipeIngredient { Food = food7, Quantity = 0.5M, Unit = "qt" },
                        new RecipeIngredient { Food = food8, Quantity = 0.5M, Unit = "qt" }
                ]
            };

            context.Recipes.Add(recipe);
            context.SaveChanges();

            context.PantryItems.Add(new PantryItem { FoodId = food1.Id, UserId = user.Id, Quantity = 3, Unit = "bunches" });
            context.PantryItems.Add(new PantryItem { FoodId = food3.Id, UserId = user.Id, Quantity = 1, Unit = "bag" });
            context.PantryItems.Add(new PantryItem { FoodId = food6.Id, Quantity = 12, UserId = user.Id });

            context.MealPlans.Add(new MealPlan
            {
                StartDate = DateTime.UtcNow.AddDays(-2),
                UserId = user.Id,
                Meals =
                [
                    new MealPlanEntry
                    {
                        Cooked = false,
                        Notes = "I am a note",
                        RecipeId = recipe.Id
                    },
                    new MealPlanEntry
                    {
                        Notes = "eat out tonight we are busy",
                    }
                ]
            });
            context.MealPlans.Add(new MealPlan
            {
                StartDate = DateTime.UtcNow,
                UserId = user.Id,
                Meals =
                [
                    new MealPlanEntry
                    {
                        Cooked = true,
                        Notes = "nachos"
                    }
                ]
            });

            context.SaveChanges();
        }
    }
}