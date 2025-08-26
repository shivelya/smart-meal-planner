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

        public static RecipeIngredientDto ToDto(this RecipeIngredient recipe)
        {
            return new RecipeIngredientDto
            {
                Ingredient = recipe.Ingredient.ToDto(),
                Quantity = recipe.Quantity,
                Unit = recipe.Unit
            };
        }

        public static IngredientDto ToDto(this Ingredient ingredient)
        {
            return new IngredientDto
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                Category = ingredient.Category.ToDto()
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
                Ingredient = entity.Ingredient.ToDto()
            };
        }
    }
}