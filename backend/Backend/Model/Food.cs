namespace Backend.Model
{
    public class Food
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
    }
}
