using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Backend.Model;
using Microsoft.Extensions.Logging;

namespace Backend.Tests
{
    [Collection("NonParallelTests")]
    public class PlannerContextTests
    {
        [Fact]
        public void CanCreatePlannerContext()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context);
        }

        [Fact]
        public void PlannerContext_Users_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Users")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.Users);
        }

        [Fact]
        public void PlannerContext_Categories_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Categories")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.Categories);
        }

        [Fact]
        public void PlannerContext_Foods_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.Foods);
        }

        [Fact]
        public void PlannerContext_PantryItems_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_PantryItems")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.PantryItems);
        }

        [Fact]
        public void PlannerContext_Recipes_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Recipes")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.Recipes);
        }

        [Fact]
        public void PlannerContext_RecipeIngredients_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_RecipeIngredients")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.RecipeIngredients);
        }

        [Fact]
        public void PlannerContext_MealPlans_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_MealPlans")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.MealPlans);
        }

        [Fact]
        public void PlannerContext_MealPlanEntries_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_MealPlanEntries")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.MealPlanEntries);
        }

        [Fact]
        public void PlannerContext_RefreshTokens_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_RefreshTokens")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.RefreshTokens);
        }

        [Fact]
        public void PlannerContext_ShoppingListItems_Property_Works()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_ShoppingListItems")
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            Assert.NotNull(context.ShoppingListItems);
        }
    }

    public class TestPlannerContext : PlannerContext
    {
        public TestPlannerContext(DbContextOptions<PlannerContext> options, IConfiguration config, ILogger<PlannerContext> logger)
            : base(options, config, logger) { }

        public void CallOnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }

    [Collection("NonParallelTests")]
    public class TestPlannerContextTests
    {
        private static ModelBuilder InitializeContext()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<PlannerContext>()
                            .UseInMemoryDatabase(dbName)
                            .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new TestPlannerContext(options, config, logger);
            context.Database.EnsureDeleted();
            var builder = new ModelBuilder();
            context.CallOnModelCreating(builder);
            Assert.NotNull(builder);
            return builder;
        }

        [Fact]
        public void FoodRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(Food));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(Food.Category)));
        }

        [Fact]
        public void PantryItemRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(PantryItem));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(PantryItem.User)));
            Assert.NotNull(entity.FindNavigation(nameof(PantryItem.Food)));
        }

        [Fact]
        public void RecipeRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(Recipe));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(Recipe.User)));
        }

        [Fact]
        public void RecipeIngredientRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(RecipeIngredient));
            Assert.NotNull(entity);

            var key = entity.FindPrimaryKey();
            Assert.NotNull(key);

            Assert.Equal(2, key.Properties.Count); // Composite key
            Assert.NotNull(entity.FindNavigation(nameof(RecipeIngredient.Recipe)));
            Assert.NotNull(entity.FindNavigation(nameof(RecipeIngredient.Food)));
        }

        [Fact]
        public void MealPlanRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(MealPlan));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(MealPlan.User)));
        }

        [Fact]
        public void MealPlanEntryRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(MealPlanEntry));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(MealPlanEntry.MealPlan)));
            Assert.NotNull(entity.FindNavigation(nameof(MealPlanEntry.Recipe)));
        }

        [Fact]
        public void RecipeEntity_HasIndexOnTitle()
        {
            var builder = InitializeContext();

            var index = builder.Model.FindEntityType(typeof(Recipe))!
                .GetIndexes()
                .FirstOrDefault(i => i.GetDatabaseName() == "IX_Recipe_Title");
            Assert.NotNull(index);
            Assert.Contains(index.Properties, p => p.Name == "Title");
        }

        [Fact]
        public void FoodEntity_HasIndexOnName()
        {
            var builder = InitializeContext();

            var index = builder.Model.FindEntityType(typeof(Food))!
                .GetIndexes()
                .FirstOrDefault(i => i.GetDatabaseName() == "IX_Food_Name");
            Assert.NotNull(index);
            Assert.Contains(index.Properties, p => p.Name == "Name");
        }

        [Fact]
        public void PantryItemEntity_HasIndexOnUserIdFoodId()
        {
            var builder = InitializeContext();

            var index = builder.Model.FindEntityType(typeof(PantryItem))!
                .GetIndexes()
                .FirstOrDefault(i => i.GetDatabaseName() == "IX_PantryItem_UserIdFoodId");
            Assert.NotNull(index);
            Assert.Contains(index.Properties, p => p.Name == "UserId");
            Assert.Contains(index.Properties, p => p.Name == "FoodId");
        }

        [Fact]
        public void ShoppingListItemRelationships_AreConfigured()
        {
            var builder = InitializeContext();

            var entity = builder.Model.FindEntityType(typeof(ShoppingListItem));
            Assert.NotNull(entity);

            Assert.NotNull(entity.FindNavigation(nameof(ShoppingListItem.User)));
            Assert.NotNull(entity.FindNavigation(nameof(ShoppingListItem.Food)));
        }
    }

    public class CategorySeedingPlannerContext : PlannerContext
    {
        public CategorySeedingPlannerContext(DbContextOptions<PlannerContext> options, IConfiguration configuration, ILogger<PlannerContext> logger)
            : base(options, configuration, logger) { }
        
        public void CallOnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }

    public class CategorySeedingPlannerContextTests
    {
        [Fact]
        public void Category_IsSeeded_FromConfiguration()
        {
            string[] values = ["Veggies", "Fruit"];
            var names = values;
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Categories:0:Id"] = "1",
                ["Categories:0:Name"] = values[0],

                ["Categories:1:Id"] = "2",
                ["Categories:1:Name"] = values[1]
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();

            // Act: create context and ensure database is created
            using (var context = new CategorySeedingPlannerContext(options, config, logger))
            {
                context.CallOnModelCreating(new ModelBuilder());
                var seededCategories = context.Categories.ToList();
                // Assert: check that all categories are seeded
                Assert.Equal(values.Length, seededCategories.Count);
                foreach (var name in values)
                    Assert.Contains(seededCategories, c => c.Name == name);

                Assert.Equal(values.Length, seededCategories.Count);
            }
        }
    }
}
