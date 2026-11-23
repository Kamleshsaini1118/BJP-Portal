using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockPortal.Core.DTOs.Auth;
using StockPortal.Core.DTOs.Damage;
using StockPortal.Core.DTOs.Distributions;
using StockPortal.Core.DTOs.FixedAssets;
using StockPortal.Core.DTOs.FoodPackets;
using StockPortal.Core.DTOs.GoodsReturn;
using StockPortal.Core.DTOs.Issues;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.DTOs.PurchaseOrders;
using StockPortal.Core.DTOs.Purchases;
using StockPortal.Core.DTOs.Quotations;
using StockPortal.Core.DTOs.Reports;
using StockPortal.Core.DTOs.Utilities;
using StockPortal.Core.DTOs.Vendors;

namespace StockPortal.Core.Interfaces
{
    public interface IAuthRepository
    {
        Task<(bool Success, string? FullName, bool IsActive, bool PasswordMatches, string? Role)> ValidateLoginAsync(string idOrMobile, byte[] passwordHash);
        Task<UserProfileDto?> GetUserProfileAsync(string membershipId);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(string membershipId, string fullName, string role);
        string GenerateRefreshToken();
    }

    public interface IPasswordHasher
    {
        byte[] HashSha256(string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public interface IProductRepository
    {
        Task<List<ProductCategoryDto>> GetCategoriesAsync(string? searchQuery = null, string statusFilter = "ALL");
        Task<ProductCategoryDto> AddCategoryAsync(string name, string? customPrefix = null, bool isActive = true);
        Task<bool> ToggleCategoryStatusAsync(int id, bool isActive);
        Task<List<ProductDto>> GetProductsAsync();
        Task<(int Id, string ProductCode)> SaveProductAsync(SaveProductDto product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> ToggleProductStatusAsync(int id, bool isActive);
    }

    public interface IVendorRepository
    {
        Task<List<VendorDto>> GetVendorsAsync();
        Task<VendorDto?> GetVendorByIdAsync(int id);
        Task<int> SaveVendorAsync(SaveVendorDto vendor);
        Task<bool> UpdateVendorAsync(int id, SaveVendorDto vendor);
        Task<bool> ToggleVendorStatusAsync(int id, bool isActive);
    }

    public interface IPurchaseRepository
    {
        Task<List<PurchaseDto>> GetPurchasesAsync(string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? paymentMode = null, string? poNumber = null, string? filterMode = null);
        Task<List<PurchaseItemDto>> GetPurchaseItemsAsync(int purchaseId);
        Task<int> SavePurchaseAsync(SavePurchaseRequestDto request);
        Task<bool> DeleteIncompletePurchaseAsync(int purchaseId, int? distributionId = null);
        Task<List<PendingChallanDto>> GetPendingChallansByVendorAsync(string vendorName);
        Task<bool> LinkChallanToInvoiceAsync(int id, string invoiceNumber, DateTime? invoiceDate, int invoiceGroupId, string? appendRemark);
        Task<string?> CheckExistingSerialNumberAsync(IEnumerable<string> serialNumbers, int? currentPurchaseId = null);
        Task<List<InvoiceFileDto>> GetInvoiceFilesAsync(int purchaseId);
        Task<int> AddInvoiceFileAsync(int purchaseId, string fileName, byte[] data);
        Task<bool> DeleteInvoiceFileAsync(int fileId);
    }

    public interface IPurchaseOrderRepository
    {
        Task<List<PurchaseOrderDto>> GetOrdersAsync(string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? productName = null);
        Task<List<PurchaseOrderItemDisplayDto>> GetItemsAsync(int purchaseOrderId);
        Task<(int Id, string PoReference)> SaveOrderAsync(SavePurchaseOrderDto dto);
    }

    public interface IFixedAssetRepository
    {
        Task<List<FixedAssetSummaryDto>> GetSummaryAsync();
        Task<List<AssetDistributionDetailDto>> GetDistributionDetailAsync(string itemName);
        Task<List<KaryalyaDto>> GetKaryalyasAsync();
        Task<int> SaveKaryalyaAsync(KaryalyaDto karyalya);
        Task<List<DepartmentDto>> GetDepartmentsByKaryalyaAsync(int karyalyaId);
        Task<int> SaveDepartmentAsync(DepartmentDto department);
        Task<List<FixedAssetRoomDto>> GetFixedAssetRoomsAsync(string? searchQuery = null);
        Task<List<ProductDto>> GetFixedAssetProductsListAsync();
        Task<List<FixedAssetRoomItemDto>> GetRoomItemsAsync(int roomId);
        Task<int> SaveRoomMappingAsync(FixedAssetRoomDto room, List<FixedAssetRoomItemDto> items);
        Task<List<FixedAssetReportItemDto>> GetFixedAssetReportItemsAsync();
        Task<List<FixedAssetHistoryRecordDto>> GetItemHistoryAsync(int itemId);
        Task<bool> MoveAssetItemAsync(int itemId, int toRoomId, string performedBy, string? remarks);
        Task<bool> UpdateItemStatusAsync(int itemId, string newStatus);
    }

    public interface IDamageRepository
    {
        Task<List<DamageRecordDto>> GetRecordsAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<DamageRecordDto?> GetRecordByIdAsync(int id);
        Task<(int Id, string DamageCode)> AddRecordAsync(CreateDamageRecordDto dto);
        Task<bool> CheckDuplicateAsync(CreateDamageRecordDto dto);
    }

    public interface IFoodPacketRepository
    {
        Task<List<FoodPacketBatchDto>> GetBatchesAsync();
        Task<(int Id, string BatchCode)> AddBatchAsync(CreateFoodPacketBatchDto dto);
        Task<bool> DispatchAsync(int batchId, string personName, string personNumber, decimal qty, DateTime dispatchDate);
        Task<List<BatchDispatchRecordDto>> GetDispatchesAsync(int batchId);
        Task<List<FoodPacketLedgerDto>> GetLedgerAsync();
    }

    public interface IGoodsReturnRepository
    {
        Task<List<GoodsReturnDto>> GetGoodsReturnsAsync(
            string? vendor = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? category = null,
            string? productName = null,
            string? reason = null,
            string? searchQuery = null);
        Task<(GoodsReturnDto? Header, List<GoodsReturnItemDto> Items)> GetGoodsReturnDetailsAsync(int id);
        Task<List<ReturnablePurchaseOptionDto>> GetVendorPurchasesForReturnAsync(string vendorName);
        Task<List<ReturnablePurchaseItemRowDto>> GetPurchaseItemsForReturnAsync(int purchaseId);
        Task<(int Id, string ReturnNo)> SaveGoodsReturnAsync(SaveGoodsReturnDto dto);
    }

    public interface IIssueRepository
    {
        Task<List<IssueRecordLineDto>> GetRecordsAsync(string? status, DateTime? dateFrom, DateTime? dateTo);
        Task<IssueRecordHeaderDto?> GetHeaderAsync(int id);
        Task<List<IssueItemRowDto>> GetItemsAsync(int issueRecordId);
        Task<(int Id, string IssueCode)> SaveIssueAsync(SaveIssueDto dto);
        Task<bool> MarkDepositedAsync(int id, DateTime depositedOn, List<DepositItemEntryDto> items);
    }

    public interface IDistributionRepository
    {
        Task<List<DistributionSummaryDto>> GetDistributionsAsync();
        Task<List<DistributionItemRowDto>> GetItemsAsync(int distributionId);
        Task<(int Id, string ShipmentId)> SaveDistributionAsync(SaveDistributionDto dto);
        Task<List<DistributionOfficeDto>> GetOfficesAsync();
        Task<DistributionOfficeDto> AddOfficeAsync(string name, string? address, string? contactPerson, string? contactNumber);
    }

    public interface IUtilityRepository
    {
        Task<List<UtilityExpenseDto>> GetExpensesAsync(string? searchQuery = null);
        Task<int> SaveExpenseAsync(SaveUtilityExpenseDto dto);
        Task<bool> DeleteExpenseAsync(int expenseId);
        Task<List<UtilityReminderDto>> GetRemindersAsync(string? searchQuery = null);
        Task<int> SaveReminderAsync(SaveUtilityReminderDto dto);
        Task<bool> DeleteReminderAsync(int reminderId);
    }

    public interface IQuotationRepository
    {
        Task<List<QuotationProjectDto>> GetProjectsAsync(string searchKeyword = "");
        Task<QuotationProjectDto?> GetProjectByIdAsync(int id);
        Task<int> SaveProjectAsync(SaveQuotationProjectDto project);
        Task<bool> DeleteProjectAsync(int projectId);
        Task<List<VendorQuotationDto>> GetQuotationsForProjectAsync(int projectId);
        Task<int> SaveQuotationAsync(SaveVendorQuotationDto quotation);
        Task<bool> UpdateQuotationStatusAsync(int quotationId, string newStatus);
        Task<bool> DeleteQuotationAsync(int quotationId);
    }

    public interface IReportRepository
    {
        Task<List<StockReportItemDto>> GetStockReportAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<List<LedgerEntryDto>> GetProductLedgerAsync(string productName);
        Task<List<PurchaseItemReportRowDto>> GetPurchaseItemsReportAsync(string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? category = null);
    }
}
