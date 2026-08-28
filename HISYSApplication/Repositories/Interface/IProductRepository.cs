using HISYSApplication.DTO;

namespace HISYSApplication.Repositories.Interface
{
    public interface IProductRepository
    {
        Task<int> AddProductAsync(ProductRequestDto product, byte[] imageBytes, string contentType);

        Task<List<ProductResponseDto>> GetAllProductsAsync();

        Task<ProductResponseDto?> GetProductAsync(int id);

        Task<ProductImageDto?> GetProductImageAsync(int id);
    }
}
