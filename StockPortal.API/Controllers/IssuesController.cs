using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Issues;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly IIssueRepository _issueRepo;

        public IssuesController(IIssueRepository issueRepo)
        {
            _issueRepo = issueRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<IssueRecordLineDto>>>> GetRecords(
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo)
        {
            var records = await _issueRepo.GetRecordsAsync(status, dateFrom, dateTo);
            return Ok(ApiResponse<List<IssueRecordLineDto>>.Ok(records));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<IssueRecordHeaderDto>>> GetHeader(int id)
        {
            var header = await _issueRepo.GetHeaderAsync(id);
            if (header == null)
            {
                return NotFound(ApiResponse<IssueRecordHeaderDto>.Fail("Issue record not found."));
            }
            return Ok(ApiResponse<IssueRecordHeaderDto>.Ok(header));
        }

        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<ApiResponse<List<IssueItemRowDto>>>> GetItems(int id)
        {
            var items = await _issueRepo.GetItemsAsync(id);
            return Ok(ApiResponse<List<IssueItemRowDto>>.Ok(items));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> SaveIssue([FromBody] SaveIssueDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IssuedTo) || string.IsNullOrWhiteSpace(dto.Purpose))
            {
                return BadRequest(ApiResponse<object>.Fail("Receiver name and purpose are required."));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("At least one issue item is required."));
            }

            var (id, code) = await _issueRepo.SaveIssueAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { Id = id, IssueCode = code }, "Issue record saved successfully."));
        }

        [HttpPost("deposit")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkDeposited([FromBody] MarkDepositedRequestDto request)
        {
            var success = await _issueRepo.MarkDepositedAsync(request.IssueRecordId, request.DepositedOn, request.Items);
            return Ok(ApiResponse<bool>.Ok(success, "Deposit processed successfully."));
        }
    }
}
