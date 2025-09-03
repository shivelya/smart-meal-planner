using Microsoft.EntityFrameworkCore;
using Backend.Model;

namespace Backend
{
    public class PlannerContext : DbContext
    {
        public PlannerContext(DbContextOptions<PlannerContext> options, IConfiguration configuration, ILogger<PlannerContext> logger)
            : base(options)
        {
            _configuration = configuration;
            _logger = logger;
            _logger.LogInformation("PlannerContext created with options: {Options}", options);
            _logger.LogInformation("Using connection string: {ConnectionString}", _configuration.GetConnectionString("DefaultConnection"));
            Database.EnsureCreated();
            _logger.LogInformation("PlannerContext initialized with connection string: {ConnectionString}", _configuration.GetConnectionString("DefaultConnection"));
        }

        private readonly IConfiguration _configuration;
        private readonly ILogger<PlannerContext> _logger;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Food> Foods { get; set; } = null!;
        public DbSet<PantryItem> PantryItems { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; } = null!;
        public DbSet<MealPlan> MealPlans { get; set; } = null!;
        public DbSet<MealPlanEntry> MealPlanEntries { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationships
            SetUpFoodRelationships(modelBuilder);
            SetUpPantryItemRelationships(modelBuilder);
            SetUpRecipeRelationships(modelBuilder);
            SetUpRecipeIngredientRelationships(modelBuilder);
            SetUpMealPlanRelationships(modelBuilder);
            SetUpMealPlanEntryRelationships(modelBuilder);
            SeedCategories(modelBuilder);

            _logger.LogInformation("Model creating completed with configured relationships and seeded categories.");
        }

        private void SeedCategories(ModelBuilder modelBuilder)
        {
            var categories = _configuration.GetSection("Categories").Get<Category[]>();
            if (categories != null && categories.Length > 0)
                modelBuilder.Entity<Category>().HasData(categories);
        }

        private static void SetUpMealPlanEntryRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MealPlanEntry>()
                .HasOne(mpe => mpe.MealPlan)
                .WithMany(mp => mp.MealPlanEntries)
                .HasForeignKey(mpe => mpe.MealPlanId);

            modelBuilder.Entity<MealPlanEntry>()
                .HasOne(mpe => mpe.Recipe)
                .WithMany(r => r.MealPlanEntries)
                .HasForeignKey(mpe => mpe.RecipeId);
        }

        private static void SetUpMealPlanRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MealPlan>()
                .HasOne(mp => mp.User)
                .WithMany(u => u.MealPlans)
                .HasForeignKey(mp => mp.UserId);
        }

        private static void SetUpRecipeIngredientRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecipeIngredient>()
                .HasKey(ri => new { ri.RecipeId, ri.FoodId });

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(ri => ri.RecipeId);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Food)
                .WithMany()
                .HasForeignKey(ri => ri.FoodId);
        }

        private static void SetUpRecipeRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<Recipe>()
                .HasIndex(r => r.Title)
                .HasDatabaseName("IX_Recipe_Title");
        }

        private static void SetUpPantryItemRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PantryItem>()
                .HasOne(p => p.User)
                .WithMany(u => u.PantryItems)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<PantryItem>()
                .HasOne(p => p.Food)
                .WithMany()
                .HasForeignKey(p => p.FoodId);

            modelBuilder.Entity<PantryItem>()
                .HasIndex(pi => new { pi.UserId, pi.FoodId })
                .HasDatabaseName("IX_PantryItem_UserIdFoodId");
        }

        private static void SetUpFoodRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Food>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId);

            modelBuilder.Entity<Food>()
                .HasIndex(p => p.Name)
                .HasDatabaseName("IX_Food_Name");
        }
    }
}
