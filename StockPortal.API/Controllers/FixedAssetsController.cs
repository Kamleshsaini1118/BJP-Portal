using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.FixedAssets;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/fixed-assets")]
    [Authorize]
    public class FixedAssetsController : ControllerBase
    {
        private readonly IFixedAssetRepository _assetRepo;

        public FixedAssetsController(IFixedAssetRepository assetRepo)
        {
            _assetRepo = assetRepo;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<List<FixedAssetSummaryDto>>>> GetSummary()
        {
            var summary = await _assetRepo.GetSummaryAsync();
            return Ok(ApiResponse<List<FixedAssetSummaryDto>>.Ok(summary));
        }

        [HttpGet("distribution-detail")]
        public async Task<ActionResult<ApiResponse<List<AssetDistributionDetailDto>>>> GetDistributionDetail([FromQuery] string itemName)
        {
            var details = await _assetRepo.GetDistributionDetailAsync(itemName);
            return Ok(ApiResponse<List<AssetDistributionDetailDto>>.Ok(details));
        }

        [HttpGet("karyalyas")]
        public async Task<ActionResult<ApiResponse<List<KaryalyaDto>>>> GetKaryalyas()
        {
            var list = await _assetRepo.GetKaryalyasAsync();
            return Ok(ApiResponse<List<KaryalyaDto>>.Ok(list));
        }

        [HttpPost("karyalyas")]
        public async Task<ActionResult<ApiResponse<int>>> SaveKaryalya([FromBody] KaryalyaDto dto)
        {
            var id = await _assetRepo.SaveKaryalyaAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Karyalya saved successfully."));
        }

        [HttpGet("departments")]
        public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments([FromQuery] int karyalyaId)
        {
            var list = await _assetRepo.GetDepartmentsByKaryalyaAsync(karyalyaId);
            return Ok(ApiResponse<List<DepartmentDto>>.Ok(list));
        }

        [HttpPost("departments")]
        public async Task<ActionResult<ApiResponse<int>>> SaveDepartment([FromBody] DepartmentDto dto)
        {
            var id = await _assetRepo.SaveDepartmentAsync(dto);
            return Ok(ApiResponse<int>.Ok(id, "Department saved successfully."));
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<ApiResponse<List<FixedAssetRoomDto>>>> GetRooms([FromQuery] string? searchQuery)
        {
            var rooms = await _assetRepo.GetFixedAssetRoomsAsync(searchQuery);
            return Ok(ApiResponse<List<FixedAssetRoomDto>>.Ok(rooms));
        }

        [HttpGet("rooms/{roomId:int}/items")]
        public async Task<ActionResult<ApiResponse<List<FixedAssetRoomItemDto>>>> GetRoomItems(int roomId)
        {
            var items = await _assetRepo.GetRoomItemsAsync(roomId);
            return Ok(ApiResponse<List<FixedAssetRoomItemDto>>.Ok(items));
        }

        [HttpPost("room-mapping")]
        public async Task<ActionResult<ApiResponse<int>>> SaveRoomMapping([FromBody] SaveRoomMappingDto dto)
        {
            var id = await _assetRepo.SaveRoomMappingAsync(dto.Room, dto.Items);
            return Ok(ApiResponse<int>.Ok(id, "Room asset mapping saved successfully."));
        }

        [HttpGet("products")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetFixedAssetProducts()
        {
            var products = await _assetRepo.GetFixedAssetProductsListAsync();
            return Ok(ApiResponse<List<ProductDto>>.Ok(products));
        }

        [HttpGet("reports")]
        public async Task<ActionResult<ApiResponse<List<FixedAssetReportItemDto>>>> GetReportItems()
        {
            var reports = await _assetRepo.GetFixedAssetReportItemsAsync();
            return Ok(ApiResponse<List<FixedAssetReportItemDto>>.Ok(reports));
        }

        [HttpGet("history/{itemId:int}")]
        public async Task<ActionResult<ApiResponse<List<FixedAssetHistoryRecordDto>>>> GetHistory(int itemId)
        {
            var history = await _assetRepo.GetItemHistoryAsync(itemId);
            return Ok(ApiResponse<List<FixedAssetHistoryRecordDto>>.Ok(history));
        }

        [HttpPost("move")]
        public async Task<ActionResult<ApiResponse<bool>>> MoveAsset([FromBody] MoveAssetRequestDto request)
        {
            var user = User.FindFirstValue(ClaimTypes.Name) ?? "System";
            var success = await _assetRepo.MoveAssetItemAsync(request.ItemId, request.ToRoomId, user, request.Remarks);
            return Ok(ApiResponse<bool>.Ok(success, "Asset moved successfully."));
        }

        [HttpPatch("status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus([FromBody] UpdateAssetStatusDto request)
        {
            var success = await _assetRepo.UpdateItemStatusAsync(request.ItemId, request.NewStatus);
            return Ok(ApiResponse<bool>.Ok(success, "Asset status updated successfully."));
        }
    }
}
