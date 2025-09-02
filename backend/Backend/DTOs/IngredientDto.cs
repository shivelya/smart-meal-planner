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

    public class FoodReferenceDto
    {
        public AddFoodMode Mode { get; set; } // "existing" or "new"
        public int? Id { get; set; } // used if Mode == "existing"

        // used if Mode == "new"
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public CategoryDto Category { get; set; } = null!;
    }



    public class GetFoodsResult
    {
        public int TotalCount { get; set; }
        public IEnumerable<FoodReferenceDto> Items { get; set; } = [];
    }
}
