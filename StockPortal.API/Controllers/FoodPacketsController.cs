using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.FoodPackets;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/food-packets")]
    [Authorize]
    public class FoodPacketsController : ControllerBase
    {
        private readonly IFoodPacketRepository _foodRepo;

        public FoodPacketsController(IFoodPacketRepository foodRepo)
        {
            _foodRepo = foodRepo;
        }

        [HttpGet("batches")]
        public async Task<ActionResult<ApiResponse<List<FoodPacketBatchDto>>>> GetBatches()
        {
            var batches = await _foodRepo.GetBatchesAsync();
            return Ok(ApiResponse<List<FoodPacketBatchDto>>.Ok(batches));
        }

        [HttpPost("batches")]
        public async Task<ActionResult<ApiResponse<object>>> AddBatch([FromBody] CreateFoodPacketBatchDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ItemName) || dto.Qty <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Item name and quantity (> 0) are required."));
            }

            var (id, code) = await _foodRepo.AddBatchAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Id = id, BatchCode = code }, "Food packet batch created successfully."));
        }

        [HttpPost("dispatch")]
        public async Task<ActionResult<ApiResponse<bool>>> Dispatch([FromBody] DispatchFoodPacketDto dto)
        {
            if (dto.BatchId <= 0 || dto.Qty <= 0 || string.IsNullOrWhiteSpace(dto.PersonName))
            {
                return BadRequest(ApiResponse<bool>.Fail("BatchId, person name, and valid dispatch quantity are required."));
            }

            var success = await _foodRepo.DispatchAsync(dto.BatchId, dto.PersonName, dto.PersonNumber, dto.Qty, dto.DispatchDate);
            return Ok(ApiResponse<bool>.Ok(success, "Dispatched successfully."));
        }

        [HttpGet("batches/{batchId:int}/dispatches")]
        public async Task<ActionResult<ApiResponse<List<BatchDispatchRecordDto>>>> GetDispatches(int batchId)
        {
            var dispatches = await _foodRepo.GetDispatchesAsync(batchId);
            return Ok(ApiResponse<List<BatchDispatchRecordDto>>.Ok(dispatches));
        }

        [HttpGet("ledger")]
        public async Task<ActionResult<ApiResponse<List<FoodPacketLedgerDto>>>> GetLedger()
        {
            var ledger = await _foodRepo.GetLedgerAsync();
            return Ok(ApiResponse<List<FoodPacketLedgerDto>>.Ok(ledger));
        }
    }
}
