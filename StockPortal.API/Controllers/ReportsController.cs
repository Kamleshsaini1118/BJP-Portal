using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.API.Services;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Reports;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportRepository _reportRepo;
        private readonly IExcelService _excelService;

        public ReportsController(IReportRepository reportRepo, IExcelService excelService)
        {
            _reportRepo = reportRepo;
            _excelService = excelService;
        }

        [HttpGet("stock-summary")]
        public async Task<ActionResult<ApiResponse<List<StockReportItemDto>>>> GetStockReport([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            var report = await _reportRepo.GetStockReportAsync(dateFrom, dateTo);
            return Ok(ApiResponse<List<StockReportItemDto>>.Ok(report));
        }

        [HttpGet("product-ledger")]
        public async Task<ActionResult<ApiResponse<List<LedgerEntryDto>>>> GetProductLedger([FromQuery] string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(ApiResponse<List<LedgerEntryDto>>.Fail("Product name is required."));
            }

            var ledger = await _reportRepo.GetProductLedgerAsync(productName);
            return Ok(ApiResponse<List<LedgerEntryDto>>.Ok(ledger));
        }

        [HttpGet("purchase-items")]
        public async Task<ActionResult<ApiResponse<List<PurchaseItemReportRowDto>>>> GetPurchaseItemsReport(
            [FromQuery] string? vendor,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? category)
        {
            var report = await _reportRepo.GetPurchaseItemsReportAsync(vendor, dateFrom, dateTo, category);
            return Ok(ApiResponse<List<PurchaseItemReportRowDto>>.Ok(report));
        }

        [HttpGet("stock-summary/export")]
        public async Task<IActionResult> ExportStockSummary([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
        {
            var data = await _reportRepo.GetStockReportAsync(dateFrom, dateTo);
            if (data == null || data.Count == 0)
            {
                return BadRequest("No records available to export.");
            }

            var columns = new Dictionary<string, Func<StockReportItemDto, object?>>
            {
                ["Item"] = x => x.Item,
                ["Batches"] = x => x.Batches,
                ["Total Qty"] = x => x.TotalQty,
                ["Dispatched"] = x => x.Dispatched,
                ["Damage Qty"] = x => x.DamageQty,
                ["Issue Qty"] = x => x.IssueQty,
                ["Remaining"] = x => x.Remaining
            };

            var fileBytes = _excelService.CreateExcel("Stock Report", data, columns);
            var fileName = $"Stock_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
