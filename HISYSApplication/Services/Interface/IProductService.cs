using HISYSApplication.DTO;

namespace HISYSApplication.Services.Interface
{
    public interface IProductService
    {
        Task<int> AddProductAsync(ProductRequestDto product);

        Task<List<ProductResponseDto>> GetAllProductsAsync(string? category = null, string? search = null);

        Task<ProductResponseDto?> GetProductAsync(int id);

        Task<ProductImageDto?> GetProductImageAsync(int id);

        Task<bool> UpdateProductAsync(int id, ProductUpdateDto product);

        Task<bool> DeleteProductAsync(int id);
    }
}
