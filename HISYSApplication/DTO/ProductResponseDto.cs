namespace HISYSApplication.DTO
{
    public class ProductResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Category { get; set; } = "General";

        public string StockStatus { get; set; } = "In Stock";

        public string ImageUrl { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }
    }
}
