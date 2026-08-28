using HISYSApplication.DTO;
using HISYSApplication.Repositories.Interface;
using HISYSApplication.Services.Interface;

namespace HISYSApplication.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(
            IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<int> AddProductAsync(
            ProductRequestDto product)
        {
            if (product.Image == null || product.Image.Length == 0)
            {
                throw new ArgumentException("Image is required.");
            }

            using var memoryStream = new MemoryStream();
            await product.Image.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();
            string contentType = product.Image.ContentType;

            return await _productRepository.AddProductAsync(
                product,
                imageBytes,
                contentType);
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync(string? category = null, string? search = null)
        {
            return await _productRepository.GetAllProductsAsync(category, search);
        }

        public async Task<ProductResponseDto?> GetProductAsync(int id)
        {
            return await _productRepository.GetProductAsync(id);
        }

        public async Task<ProductImageDto?> GetProductImageAsync(int id)
        {
            return await _productRepository.GetProductImageAsync(id);
        }

        public async Task<bool> UpdateProductAsync(int id, ProductUpdateDto product)
        {
            byte[]? imageBytes = null;
            string? contentType = null;

            if (product.Image != null && product.Image.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await product.Image.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
                contentType = product.Image.ContentType;
            }

            return await _productRepository.UpdateProductAsync(id, product, imageBytes, contentType);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _productRepository.DeleteProductAsync(id);
        }
    }
}
