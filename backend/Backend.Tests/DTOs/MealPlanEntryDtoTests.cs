using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class MealPlanEntryDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var now = DateTime.Now;
            var dto = new MealPlanEntryDto { Id = 6, RecipeId = 4, Notes = "note" };
            Assert.Equal(6, dto.Id);
            Assert.Equal(4, dto.RecipeId);
            Assert.Equal("note", dto.Notes);
        }
    }
}
