using Microsoft.EntityFrameworkCore;
using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend
{
    public class PlannerContext : DbContext
    {
        public PlannerContext(DbContextOptions<PlannerContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
            Database.EnsureCreated();
        }

        private readonly IConfiguration _configuration;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Ingredient> Ingredients { get; set; } = null!;
        public DbSet<PantryItem> PantryItems { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; } = null!;
        public DbSet<MealPlan> MealPlans { get; set; } = null!;
        public DbSet<MealPlanEntry> MealPlanEntries { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationships
            SetUpIngredientRelationships(modelBuilder);
            SetUpPantryItemRelationships(modelBuilder);
            SetUpRecipeRelationships(modelBuilder);
            SetUpRecipeIngredientRelationships(modelBuilder);
            SetUpMealPlanRelationships(modelBuilder);
            SetUpMealPlanEntryRelationships(modelBuilder);
            SeedCategories(modelBuilder);
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
                .HasKey(ri => new { ri.RecipeId, ri.IngredientId });

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId);
        }

        private static void SetUpRecipeRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId);
        }

        private static void SetUpPantryItemRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PantryItem>()
                .HasOne(p => p.User)
                .WithMany(u => u.PantryItems)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<PantryItem>()
                .HasOne(p => p.Ingredient)
                .WithMany(i => i.PantryItems)
                .HasForeignKey(p => p.IngredientId);
        }

        private static void SetUpIngredientRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Ingredients)
                .HasForeignKey(i => i.CategoryId);
        }
    }
}
