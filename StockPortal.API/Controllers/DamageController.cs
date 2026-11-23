using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Damage;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/damage-records")]
    [Authorize]
    public class DamageController : ControllerBase
    {
        private readonly IDamageRepository _damageRepo;

        public DamageController(IDamageRepository damageRepo)
        {
            _damageRepo = damageRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DamageRecordDto>>>> GetRecords([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            var records = await _damageRepo.GetRecordsAsync(dateFrom, dateTo);
            return Ok(ApiResponse<List<DamageRecordDto>>.Ok(records));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<DamageRecordDto>>> GetRecordById(int id)
        {
            var record = await _damageRepo.GetRecordByIdAsync(id);
            if (record == null)
            {
                return NotFound(ApiResponse<DamageRecordDto>.Fail($"Damage record with ID {id} not found."));
            }
            return Ok(ApiResponse<DamageRecordDto>.Ok(record));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> AddRecord([FromBody] CreateDamageRecordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ItemName) || dto.Qty <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Item name and quantity (> 0) are required."));
            }

            // Duplicate entry check
            var isDuplicate = await _damageRepo.CheckDuplicateAsync(dto);
            if (isDuplicate)
            {
                var reporteeText = !string.IsNullOrWhiteSpace(dto.ReporteeName) ? $" reported by '{dto.ReporteeName}'" : "";
                return Conflict(ApiResponse<object>.Fail($"Duplicate entry detected: A damage report for '{dto.ItemName}' with quantity {dto.Qty:G29}{reporteeText} already exists today."));
            }

            var (id, code) = await _damageRepo.AddRecordAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Id = id, DamageCode = code }, "Damage record created successfully."));
        }
    }
}
