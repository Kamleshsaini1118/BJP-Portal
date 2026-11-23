using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Quotations;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class QuotationsController : ControllerBase
    {
        private readonly IQuotationRepository _quoteRepo;

        public QuotationsController(IQuotationRepository quoteRepo)
        {
            _quoteRepo = quoteRepo;
        }

        [HttpGet("projects")]
        public async Task<ActionResult<ApiResponse<List<QuotationProjectDto>>>> GetProjects([FromQuery] string searchKeyword = "")
        {
            var projects = await _quoteRepo.GetProjectsAsync(searchKeyword);
            return Ok(ApiResponse<List<QuotationProjectDto>>.Ok(projects));
        }

        [HttpGet("projects/{id:int}")]
        public async Task<ActionResult<ApiResponse<QuotationProjectDto>>> GetProject(int id)
        {
            var project = await _quoteRepo.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound(ApiResponse<QuotationProjectDto>.Fail("Quotation project not found."));
            }
            return Ok(ApiResponse<QuotationProjectDto>.Ok(project));
        }

        [HttpPost("projects")]
        public async Task<ActionResult<ApiResponse<int>>> SaveProject([FromBody] SaveQuotationProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return BadRequest(ApiResponse<int>.Fail("Project title is required."));
            }

            var id = await _quoteRepo.SaveProjectAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Project saved successfully."));
        }

        [HttpDelete("projects/{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(int id)
        {
            var success = await _quoteRepo.DeleteProjectAsync(id);
            return Ok(ApiResponse<bool>.Ok(success, "Project deleted successfully."));
        }

        [HttpGet("projects/{projectId:int}/quotes")]
        public async Task<ActionResult<ApiResponse<List<VendorQuotationDto>>>> GetQuotesForProject(int projectId)
        {
            var quotes = await _quoteRepo.GetQuotationsForProjectAsync(projectId);
            return Ok(ApiResponse<List<VendorQuotationDto>>.Ok(quotes));
        }

        [HttpPost("quotes")]
        public async Task<ActionResult<ApiResponse<int>>> SaveQuotation([FromBody] SaveVendorQuotationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VendorName) || dto.ProjectId <= 0)
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor name and project ID are required."));
            }

            var id = await _quoteRepo.SaveQuotationAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Quotation saved successfully."));
        }

        [HttpPatch("quotes/{id:int}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateQuotationStatus(int id, [FromBody] string newStatus)
        {
            var success = await _quoteRepo.UpdateQuotationStatusAsync(id, newStatus);
            return Ok(ApiResponse<bool>.Ok(success, "Quotation status updated."));
        }

        [HttpDelete("quotes/{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteQuotation(int id)
        {
            var success = await _quoteRepo.DeleteQuotationAsync(id);
            return Ok(ApiResponse<bool>.Ok(success, "Quotation deleted successfully."));
        }
    }
}
