using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Distributions;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class DistributionsController : ControllerBase
    {
        private readonly IDistributionRepository _distRepo;

        public DistributionsController(IDistributionRepository distRepo)
        {
            _distRepo = distRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DistributionSummaryDto>>>> GetDistributions()
        {
            var distributions = await _distRepo.GetDistributionsAsync();
            return Ok(ApiResponse<List<DistributionSummaryDto>>.Ok(distributions));
        }

        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<ApiResponse<List<DistributionItemRowDto>>>> GetItems(int id)
        {
            var items = await _distRepo.GetItemsAsync(id);
            return Ok(ApiResponse<List<DistributionItemRowDto>>.Ok(items));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SaveDistributionResultDto>>> SaveDistribution([FromBody] SaveDistributionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Destination))
            {
                return BadRequest(ApiResponse<SaveDistributionResultDto>.Fail("Destination is required."));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(ApiResponse<SaveDistributionResultDto>.Fail("At least one distribution item is required."));
            }

            var (id, shipmentId) = await _distRepo.SaveDistributionAsync(dto);
            return Ok(ApiResponse<SaveDistributionResultDto>.Ok(new SaveDistributionResultDto { Id = id, ShipmentId = shipmentId }, "Distribution saved successfully."));
        }

        // ================= OFFICES =================

        [HttpGet("offices")]
        public async Task<ActionResult<ApiResponse<List<DistributionOfficeDto>>>> GetOffices()
        {
            var offices = await _distRepo.GetOfficesAsync();
            return Ok(ApiResponse<List<DistributionOfficeDto>>.Ok(offices));
        }

        [HttpPost("offices")]
        public async Task<ActionResult<ApiResponse<DistributionOfficeDto>>> AddOffice([FromBody] DistributionOfficeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(ApiResponse<DistributionOfficeDto>.Fail("Office name is required."));
            }

            var office = await _distRepo.AddOfficeAsync(dto.Name, dto.Address, dto.ContactPerson, dto.ContactNumber);
            return Ok(ApiResponse<DistributionOfficeDto>.Ok(office, "Office added successfully."));
        }
    }
}
