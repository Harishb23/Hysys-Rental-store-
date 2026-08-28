using System.ComponentModel.DataAnnotations;

namespace HISYSApplication.DTO
{
    public class ProductUpdateDto
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 10000000, ErrorMessage = "Price must be non-negative.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(100)]
        public string Category { get; set; } = "General";

        [StringLength(50)]
        public string StockStatus { get; set; } = "In Stock";

        public IFormFile? Image { get; set; }
    }
}
