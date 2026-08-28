using HISYSApplication.DTO;
using HISYSApplication.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HISYSApplication.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(
            IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(
            [FromForm] ProductRequestDto product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var productId =
                await _productService.AddProductAsync(product);

            return Ok(new
            {
                id = productId,
                message = "Added Successfully"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products =
                await _productService.GetAllProductsAsync();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product =
                await _productService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound("Product not found.");
            }

            return Ok(product);
        }

        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetProductImage(int id)
        {
            var image =
                await _productService.GetProductImageAsync(id);

            if (image == null)
            {
                return NotFound("Image not found.");
            }

            return File(
                image.ImageData,
                image.ContentType);
        }
    }
}
