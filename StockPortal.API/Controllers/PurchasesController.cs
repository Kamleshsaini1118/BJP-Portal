using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Purchases;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseRepository _purchaseRepo;

        public PurchasesController(IPurchaseRepository purchaseRepo)
        {
            _purchaseRepo = purchaseRepo;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PurchaseDto>>>> GetPurchases(
            [FromQuery] string? vendor,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? paymentMode,
            [FromQuery] string? poNumber,
            [FromQuery] string? filterMode)
        {
            var purchases = await _purchaseRepo.GetPurchasesAsync(vendor, dateFrom, dateTo, paymentMode, poNumber, filterMode);
            return Ok(ApiResponse<List<PurchaseDto>>.Ok(purchases));
        }

        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<ApiResponse<List<PurchaseItemDto>>>> GetPurchaseItems(int id)
        {
            var items = await _purchaseRepo.GetPurchaseItemsAsync(id);
            return Ok(ApiResponse<List<PurchaseItemDto>>.Ok(items));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<int>>> SavePurchase([FromBody] SavePurchaseRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.VendorName))
            {
                return BadRequest(ApiResponse<int>.Fail("Vendor name is required."));
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(ApiResponse<int>.Fail("At least one purchase item is required."));
            }

            var id = await _purchaseRepo.SavePurchaseAsync(request);
            return Ok(ApiResponse<int>.Ok(id, "Purchase saved successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePurchase(int id, [FromQuery] int? distributionId)
        {
            var success = await _purchaseRepo.DeleteIncompletePurchaseAsync(id, distributionId);
            return Ok(ApiResponse<bool>.Ok(success, "Purchase deleted successfully."));
        }

        [HttpGet("pending-challans")]
        public async Task<ActionResult<ApiResponse<List<PendingChallanDto>>>> GetPendingChallans([FromQuery] string vendorName)
        {
            if (string.IsNullOrWhiteSpace(vendorName))
            {
                return BadRequest(ApiResponse<List<PendingChallanDto>>.Fail("Vendor name is required."));
            }

            var challans = await _purchaseRepo.GetPendingChallansByVendorAsync(vendorName);
            return Ok(ApiResponse<List<PendingChallanDto>>.Ok(challans));
        }

        [HttpPost("link-challan")]
        public async Task<ActionResult<ApiResponse<bool>>> LinkChallan([FromBody] LinkChallanRequestDto request)
        {
            var success = await _purchaseRepo.LinkChallanToInvoiceAsync(
                request.Id, request.InvoiceNumber, request.InvoiceDate, request.InvoiceGroupId, request.AppendRemark);

            return Ok(ApiResponse<bool>.Ok(success, "Challan linked to invoice successfully."));
        }

        [HttpPost("check-serials")]
        public async Task<ActionResult<ApiResponse<string?>>> CheckSerials([FromBody] CheckSerialNumbersRequestDto request)
        {
            var conflict = await _purchaseRepo.CheckExistingSerialNumberAsync(request.SerialNumbers, request.CurrentPurchaseId);
            return Ok(ApiResponse<string?>.Ok(conflict));
        }

        // ================= INVOICE FILES =================

        [HttpGet("{id:int}/invoices")]
        public async Task<ActionResult<ApiResponse<List<InvoiceFileDto>>>> GetInvoiceFiles(int id)
        {
            var files = await _purchaseRepo.GetInvoiceFilesAsync(id);
            return Ok(ApiResponse<List<InvoiceFileDto>>.Ok(files));
        }

        [HttpPost("{id:int}/invoices")]
        public async Task<ActionResult<ApiResponse<int>>> UploadInvoiceFile(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<int>.Fail("No file provided."));
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();

            var fileId = await _purchaseRepo.AddInvoiceFileAsync(id, file.FileName, data);
            return Ok(ApiResponse<int>.Ok(fileId, "Invoice file uploaded successfully."));
        }

        [HttpDelete("invoices/{fileId:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteInvoiceFile(int fileId)
        {
            var success = await _purchaseRepo.DeleteInvoiceFileAsync(fileId);
            return Ok(ApiResponse<bool>.Ok(success, "Invoice file deleted successfully."));
        }
    }
}
