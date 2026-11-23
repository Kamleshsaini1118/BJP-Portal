using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.PurchaseOrders;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/purchase-orders")]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderRepository _poRepo;

        public PurchaseOrdersController(IPurchaseOrderRepository poRepo)
        {
            _poRepo = poRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetOrders(
            [FromQuery] string? vendor,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? status,
            [FromQuery] string? product)
        {
            var orders = await _poRepo.GetOrdersAsync(vendor, dateFrom, dateTo, status, product);
            return Ok(ApiResponse<List<PurchaseOrderDto>>.Ok(orders));
        }

        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<ApiResponse<List<PurchaseOrderItemDisplayDto>>>> GetOrderItems(int id)
        {
            var items = await _poRepo.GetItemsAsync(id);
            return Ok(ApiResponse<List<PurchaseOrderItemDisplayDto>>.Ok(items));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> SaveOrder([FromBody] SavePurchaseOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VendorName))
            {
                return BadRequest(ApiResponse<object>.Fail("Vendor name is required."));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("At least one purchase order item is required."));
            }

            var (id, poRef) = await _poRepo.SaveOrderAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Id = id, PoReference = poRef }, "Purchase order saved successfully."));
        }
    }
}
