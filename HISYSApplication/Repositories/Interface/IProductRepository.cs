using HISYSApplication.DTO;

namespace HISYSApplication.Repositories.Interface
{
    public interface IProductRepository
    {
        Task<int> AddProductAsync(ProductRequestDto product, byte[] imageBytes, string contentType);

        Task<List<ProductResponseDto>> GetAllProductsAsync(string? category = null, string? search = null);

        Task<ProductResponseDto?> GetProductAsync(int id);

        Task<ProductImageDto?> GetProductImageAsync(int id);

        Task<bool> UpdateProductAsync(int id, ProductUpdateDto product, byte[]? newImageBytes, string? newContentType);

        Task<bool> DeleteProductAsync(int id);
    }
}
