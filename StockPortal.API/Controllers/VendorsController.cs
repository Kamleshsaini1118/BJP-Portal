using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.DTOs.Vendors;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorRepository _vendorRepo;

        public VendorsController(IVendorRepository vendorRepo)
        {
            _vendorRepo = vendorRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<VendorDto>>>> GetVendors()
        {
            var vendors = await _vendorRepo.GetVendorsAsync();
            return Ok(ApiResponse<List<VendorDto>>.Ok(vendors));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorDto>>> GetVendorById(int id)
        {
            var vendor = await _vendorRepo.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return NotFound(ApiResponse<VendorDto>.Fail("Vendor not found."));
            }
            return Ok(ApiResponse<VendorDto>.Ok(vendor));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<int>>> SaveVendor([FromBody] SaveVendorDto vendor)
        {
            if (string.IsNullOrWhiteSpace(vendor.Name))
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor name cannot be empty."));
            }

            var id = await _vendorRepo.SaveVendorAsync(vendor);
            return Ok(ApiResponse<int>.Ok(id, "Vendor saved successfully."));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<int>>> UpdateVendor(int id, [FromBody] SaveVendorDto vendor)
        {
            if (string.IsNullOrWhiteSpace(vendor.Name))
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor name cannot be empty."));
            }

            vendor.Id = id;
            var updatedId = await _vendorRepo.SaveVendorAsync(vendor);
            return Ok(ApiResponse<int>.Ok(updatedId, "Vendor details updated successfully."));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<int>>> UpdateVendorDirect([FromBody] SaveVendorDto vendor)
        {
            if (!vendor.Id.HasValue || vendor.Id.Value <= 0)
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor ID is required for editing."));
            }

            if (string.IsNullOrWhiteSpace(vendor.Name))
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor name cannot be empty."));
            }

            var updatedId = await _vendorRepo.SaveVendorAsync(vendor);
            return Ok(ApiResponse<int>.Ok(updatedId, "Vendor details updated successfully."));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id, [FromBody] ToggleStatusDto dto)
        {
            var success = await _vendorRepo.ToggleVendorStatusAsync(id, dto.IsActive);
            return Ok(ApiResponse<bool>.Ok(success, "Vendor status updated."));
        }
    }
}
