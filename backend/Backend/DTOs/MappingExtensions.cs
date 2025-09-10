using Backend.Model;

namespace Backend.DTOs
{
    public static class MappingExtensions
    {
        public static RecipeDto ToDto(this Recipe recipe)
        {
            return new RecipeDto
            {
                Id = recipe.Id,
                UserId = recipe.UserId,
                Title = recipe.Title,
                Source = recipe.Source,
                Instructions = recipe.Instructions,
                Ingredients = [.. recipe.Ingredients?.Select(ToDto)!]
            };
        }

        public static RecipeIngredientDto ToDto(this RecipeIngredient ingredient)
        {
            return new RecipeIngredientDto
            {
                Food = ingredient.Food?.ToDto()!,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit
            };
        }

        public static FoodDto ToDto(this Food food)
        {
            return new FoodDto
            {
                Id = food.Id,
                Name = food.Name,
                Category = food.Category?.ToDto()!
            };
        }

        public static CategoryDto ToDto(this Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        public static PantryItemDto ToDto(this PantryItem entity)
        {
            return new PantryItemDto
            {
                Id = entity.Id,
                Quantity = entity.Quantity,
                Unit = entity.Unit,
                FoodId = entity.FoodId,
                Food = entity.Food?.ToDto()!
            };
        }

        public static MealPlanDto ToDto(this MealPlan entity)
        {
            return new MealPlanDto
            {
                Id = entity.Id,
                StartDate = entity.StartDate,
                Meals = entity.Meals?.Select(ToDto)!
            };
        }

        public static MealPlanEntryDto ToDto(this MealPlanEntry entity)
        {
            return new MealPlanEntryDto
            {
                Id = entity.Id,
                Notes = entity.Notes,
                Cooked = entity.Cooked,
                RecipeId = entity.RecipeId,
                Recipe = entity.Recipe?.ToDto()
            };
        }
    }
}