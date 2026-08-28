namespace HISYSApplication.DTO
{
    public class ProductRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile Image { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
