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

            // Convert uploaded image to binary
            using var memoryStream = new MemoryStream();

            await product.Image.CopyToAsync(memoryStream);

            byte[] imageBytes = memoryStream.ToArray();

            // Content type: image/jpeg, image/png, image/webp, etc.
            string contentType = product.Image.ContentType;

            // Save to database through repository
            return await _productRepository.AddProductAsync(
                product,
                imageBytes,
                contentType);
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllProductsAsync();
        }

        public async Task<ProductResponseDto?> GetProductAsync(int id)
        {
            return await _productRepository.GetProductAsync(id);
        }

        public async Task<ProductImageDto?> GetProductImageAsync(int id)
        {
            return await _productRepository.GetProductImageAsync(id);
        }
    }
}
