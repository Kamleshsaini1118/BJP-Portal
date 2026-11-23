using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepo;

        public ProductsController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProducts()
        {
            var products = await _productRepo.GetProductsAsync();
            return Ok(ApiResponse<List<ProductDto>>.Ok(products));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> SaveProduct([FromBody] SaveProductDto product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return BadRequest(ApiResponse<object>.Fail("Product name cannot be empty."));
            }

            var (id, code) = await _productRepo.SaveProductAsync(product);
            return Ok(ApiResponse<object>.Ok(new { Id = id, ProductCode = code }, "Product saved successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            var success = await _productRepo.DeleteProductAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse<bool>.Fail("Product not found or already deleted."));
            }
            return Ok(ApiResponse<bool>.Ok(true, "Product deleted successfully."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id, [FromBody] ToggleStatusDto dto)
        {
            var success = await _productRepo.ToggleProductStatusAsync(id, dto.IsActive);
            return Ok(ApiResponse<bool>.Ok(success, "Product status updated."));
        }

        // ================= CATEGORIES =================

        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<ProductCategoryDto>>>> GetCategories([FromQuery] string? searchQuery, [FromQuery] string statusFilter = "ALL")
        {
            var categories = await _productRepo.GetCategoriesAsync(searchQuery, statusFilter);
            return Ok(ApiResponse<List<ProductCategoryDto>>.Ok(categories));
        }

        [HttpPost("categories")]
        public async Task<ActionResult<ApiResponse<ProductCategoryDto>>> AddCategory([FromBody] CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<ProductCategoryDto>.Fail("Category name cannot be empty."));
            }

            var category = await _productRepo.AddCategoryAsync(dto.Name, dto.CustomPrefix, dto.IsActive);
            return Ok(ApiResponse<ProductCategoryDto>.Ok(category, "Category added successfully."));
        }

        [HttpPatch("categories/{id:int}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleCategoryStatus(int id, [FromBody] ToggleStatusDto dto)
        {
            var success = await _productRepo.ToggleCategoryStatusAsync(id, dto.IsActive);
            return Ok(ApiResponse<bool>.Ok(success, "Category status updated."));
        }
    }
}
