using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;

namespace CryoablationSoftware.Services
{
    public interface IInventoryService
    {
        // Item Management
        Task<IEnumerable<Item>> GetAllItemsAsync();
        Task<Item> GetItemByIdAsync(Guid itemId);
        Task<Item> GetItemByIdAsync2(Guid itemId);
        Task<bool> CreateItemAsync(ItemCreateViewModel model, string userId);
      Task<bool> UpdateItemAsync(ItemEditViewModel model);
        Task<bool> DeleteItemAsync(Guid itemId);
        Task<bool> IsItemCodeUniqueAsync(string itemCode, Guid? excludeItemId = null);
        Task<ExcelUploadViewModels> UploadExcelDataAsync(IFormFile excelFile, string userId);

        Task<IEnumerable<ItemGroupViewModel>> GetGroupedItemsAsync();
        Task<int> GetGroupedItemStockAsync(string itemName);
        Task<IEnumerable<ItemVariantViewModel>> GetItemVariantsByNameAsync(string itemName);


        // Supplier Management
        //Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        //Task<Supplier> GetSupplierByIdAsync(Guid supplierId);
        //Task<bool> CreateSupplierAsync(SupplierCreateViewModel model);

        //  Stock Management
        Task<bool> ReceiveStockAsync(StockReceiveViewModel model, string userId);
        Task<bool> IssueStockAsync(StockIssueViewModel model, string userId);
        Task<int> GetCurrentStockAsync(Guid itemId);
        Task<IEnumerable<StockRecord>> GetStockHistoryAsync(Guid itemId);
        Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid itemId);

//        Reports & Analytics
       Task<InventoryDashboardViewModel> GetDashboardDataAsync();
        Task<IEnumerable<ItemStockReportViewModel>> GetStockReportAsync();
        Task<IEnumerable<LowStockItemViewModel>> GetLowStockItemsAsync();

        // Utility Methods
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<IEnumerable<string>> GetUnitsAsync();
    }
}
