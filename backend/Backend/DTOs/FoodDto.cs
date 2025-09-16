using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AddFoodMode
    {
        [JsonPropertyName("existing")]
        Existing,
        [JsonPropertyName("new")]
        New
    }
    public class FoodDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required int CategoryId { get; set; }
        public required CategoryDto Category { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
    [JsonDerivedType(typeof(NewFoodReferenceDto), nameof(AddFoodMode.New))]
    [JsonDerivedType(typeof(ExistingFoodReferenceDto), nameof(AddFoodMode.Existing))]
    public abstract class FoodReferenceDto
    {
        [Required]
        public AddFoodMode Mode { get; set; }
    }

    public class NewFoodReferenceDto : FoodReferenceDto
    {
        public NewFoodReferenceDto()
        {
            Mode = AddFoodMode.New;
        }

        public required string Name { get; set; }
        public required int CategoryId { get; set; }
    }

    public class ExistingFoodReferenceDto : FoodReferenceDto
    {
        public ExistingFoodReferenceDto()
        {
            Mode = AddFoodMode.Existing;
        }

        public required int Id { get; set; }
    }

    public class GetFoodsResult
    {
        public required int TotalCount { get; set; }
        public required IEnumerable<FoodDto> Items { get; set; } = [];
    }
}
