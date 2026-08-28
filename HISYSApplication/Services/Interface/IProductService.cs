using HISYSApplication.DTO;

namespace HISYSApplication.Services.Interface
{
    public interface IProductService
    {
        Task<int> AddProductAsync(ProductRequestDto product);

        Task<List<ProductResponseDto>> GetAllProductsAsync();

        Task<ProductResponseDto?> GetProductAsync(int id);

        Task<ProductImageDto?> GetProductImageAsync(int id);
    }
}
