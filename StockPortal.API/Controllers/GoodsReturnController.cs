using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.GoodsReturn;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/goods-returns")]
    [Authorize]
    public class GoodsReturnController : ControllerBase
    {
        private readonly IGoodsReturnRepository _returnRepo;

        public GoodsReturnController(IGoodsReturnRepository returnRepo)
        {
            _returnRepo = returnRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<GoodsReturnDto>>>> GetReturns(
            [FromQuery] string? vendor,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? category,
            [FromQuery] string? productName,
            [FromQuery] string? reason,
            [FromQuery] string? searchQuery)
        {
            var returns = await _returnRepo.GetGoodsReturnsAsync(vendor, dateFrom, dateTo, category, productName, reason, searchQuery);
            return Ok(ApiResponse<List<GoodsReturnDto>>.Ok(returns));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<object>>> GetReturnDetails(int id)
        {
            var (header, items) = await _returnRepo.GetGoodsReturnDetailsAsync(id);
            if (header == null)
            {
                return NotFound(ApiResponse<object>.Fail("Goods return record not found."));
            }

            header.Items = items;
            return Ok(ApiResponse<GoodsReturnDto>.Ok(header));
        }

        [HttpGet("vendor-purchases")]
        public async Task<ActionResult<ApiResponse<List<ReturnablePurchaseOptionDto>>>> GetVendorPurchases([FromQuery] string vendorName)
        {
            if (string.IsNullOrWhiteSpace(vendorName))
            {
                return BadRequest(ApiResponse<List<ReturnablePurchaseOptionDto>>.Fail("Vendor name is required."));
            }

            var options = await _returnRepo.GetVendorPurchasesForReturnAsync(vendorName);
            return Ok(ApiResponse<List<ReturnablePurchaseOptionDto>>.Ok(options));
        }

        [HttpGet("purchases/{purchaseId:int}/items")]
        public async Task<ActionResult<ApiResponse<List<ReturnablePurchaseItemRowDto>>>> GetPurchaseItems(int purchaseId)
        {
            var items = await _returnRepo.GetPurchaseItemsForReturnAsync(purchaseId);
            return Ok(ApiResponse<List<ReturnablePurchaseItemRowDto>>.Ok(items));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> SaveReturn([FromBody] SaveGoodsReturnDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VendorName) || dto.PurchaseId <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Vendor name and valid purchase ID are required."));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("At least one return item is required."));
            }

            var (id, returnNo) = await _returnRepo.SaveGoodsReturnAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Id = id, ReturnNo = returnNo }, "Goods return saved successfully."));
        }
    }
}
