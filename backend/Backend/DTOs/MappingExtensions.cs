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
                Ingredients = [.. recipe.Ingredients.Select(ToDto)]
            };
        }

        public static RecipeIngredientDto ToDto(this RecipeIngredient ingredient)
        {
            return new RecipeIngredientDto
            {
                Food = ingredient.Food.ToDto(),
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit
            };
        }

        public static FoodReferenceDto ToDto(this Food food)
        {
            return new FoodReferenceDto
            {
                Id = food.Id,
                Name = food.Name,
                CategoryId = food.CategoryId,
                Category = food.Category.ToDto()
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
                UserId = entity.UserId,
                Food = entity.Food.ToDto()
            };
        }
    }
}