using HISYSApplication.DTO;
using HISYSApplication.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HISYSApplication.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Retrieve all products with optional category and keyword filtering.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts([FromQuery] string? category = null, [FromQuery] string? search = null)
        {
            var products = await _productService.GetAllProductsAsync(category, search);
            return Ok(products);
        }

        /// <summary>
        /// Retrieve single product by ID.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            return Ok(product);
        }

        /// <summary>
        /// Stream product image binary data directly.
        /// </summary>
        [HttpGet("{id}/image")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductImage(int id)
        {
            var image = await _productService.GetProductImageAsync(id);

            if (image == null)
            {
                return NotFound(new { message = "Image not found." });
            }

            return File(image.ImageData, image.ContentType);
        }

        /// <summary>
        /// Admin endpoint: Add a new store product with image.
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddProduct([FromForm] ProductRequestDto product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var productId = await _productService.AddProductAsync(product);

            return Ok(new
            {
                id = productId,
                message = "Product added successfully."
            });
        }

        /// <summary>
        /// Admin endpoint: Update an existing product (with optional new image).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateDto product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _productService.UpdateProductAsync(id, product);
            if (!updated)
            {
                return NotFound(new { message = "Product not found or update failed." });
            }

            return Ok(new
            {
                id = id,
                message = "Product updated successfully."
            });
        }

        /// <summary>
        /// Admin endpoint: Delete a product by ID.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Product not found or delete failed." });
            }

            return Ok(new
            {
                id = id,
                message = "Product deleted successfully."
            });
        }
    }
}
