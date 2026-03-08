using ClosedXML.Excel;
using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public InventoryService(ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        #region Grouped Items

        public async Task<IEnumerable<ItemGroupViewModel>> GetGroupedItemsAsync()
        {
            var items = await _context.Items
                .Include(i => i.CreatedBy)
                .Where(i => i.IsActive)
                .ToListAsync();

            var groupedItems = items
                .GroupBy(i => i.ItemName)
                .Select(g => new ItemGroupViewModel
                {
                    Category = g.First().Category,
                    ItemName = g.Key,
                    Unit = g.First().Unit,
                    TotalVariants = g.Count(),
                    ReorderLevel = g.Min(i => i.ReorderLevel),
                    HasActiveItems = g.Any(i => i.IsActive),
                    LatestCreatedDate = g.Max(i => i.CreatedAt),
                    CreatedByUser = g.OrderByDescending(i => i.CreatedAt)
                                    .First().CreatedBy?.UserName ?? "System",
                    Variants = g.Select(i => new ItemVariantViewModel
                    {
                        ItemId = i.ItemId,
                        ItemCode = i.ItemCode,
                        ItemName = i.ItemName,
                        Category = i.Category,
                        Unit = i.Unit,
                        CurrentStock = 0, // Will be loaded separately
                        ReorderLevel = i.ReorderLevel,
                        IsActive = i.IsActive,
                        CreatedByUser = i.CreatedBy?.UserName ?? "System",
                        CreatedAt = i.CreatedAt
                    }).ToList()
                })
                .OrderBy(g => g.ItemName)
                .ToList();

            return groupedItems;
        }

        public async Task<int> GetGroupedItemStockAsync(string itemName)
        {
            var itemIds = await _context.Items
                .Where(i => i.ItemName == itemName && i.IsActive)
                .Select(i => i.ItemId)
                .ToListAsync();

            int totalStock = 0;
            foreach (var itemId in itemIds)
            {
                var stock = await GetCurrentStockAsync(itemId);
                totalStock += stock;
            }

            return totalStock;
        }

        public async Task<IEnumerable<ItemVariantViewModel>> GetItemVariantsByNameAsync(string itemName)
        {
            var items = await _context.Items
                .Include(i => i.CreatedBy)
                .Where(i => i.ItemName == itemName)
                .ToListAsync();

            var variants = new List<ItemVariantViewModel>();

            foreach (var item in items)
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);

                variants.Add(new ItemVariantViewModel
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Category = item.Category,
                    Unit = item.Unit,
                    CurrentStock = currentStock,
                    ReorderLevel = item.ReorderLevel,
                    IsActive = item.IsActive,
                    CreatedByUser = item.CreatedBy?.UserName ?? "System",
                    CreatedAt = item.CreatedAt
                });
            }

            return variants;
        }

        #endregion

        #region Item Management

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _context.Items
                //  .Where(i => i.IsActive)
                .Include(i => i.CreatedBy)
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }


        public async Task<Item> GetItemByIdAsync(Guid itemId)
        {
            return await _context.Items
                .Include(i => i.CreatedBy)
                .Include(i => i.StockRecords.Where(sr => sr.IsActive))
                .Include(i => i.UsageRecords.Where(ur => ur.IsActive))
                    .ThenInclude(ur => ur.Patient)
                .FirstOrDefaultAsync(i => i.ItemId == itemId && i.IsActive);
        }

        public async Task<Item> GetItemByIdAsync2(Guid itemId)
        {
            return await _context.Items
                .Include(i => i.CreatedBy)
                .Include(i => i.StockRecords.Where(sr => sr.IsActive))
                .Include(i => i.UsageRecords.Where(ur => ur.IsActive))
                    .ThenInclude(ur => ur.Patient)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);
        }

        public async Task<bool> CreateItemAsync(ItemCreateViewModel model, string userId)
        {
            try
            {
                var item = new Item
                {
                    ItemId = Guid.NewGuid(),
                    ItemName = model.ItemName.Trim(),
                    ItemCode = model.ItemCode.Trim().ToUpper(),
                    Category = model.Category,
                    Unit = model.Unit,
                    ItemType = model.ItemType,  // YE ADD KARO
                    ReorderLevel = model.ReorderLevel,
                    Description = model.Description?.Trim(),
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // Log successful item creation
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "CREATE",
                    tableName: "Items",
                    recordId: item.ItemId.ToString(),
                    newValues: new { ItemId = item.ItemId, ItemCode = item.ItemCode, ItemName = item.ItemName, Category = item.Category },
                    customMessage: $"Item created: {item.ItemName}"
                );

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<bool> UpdateItemAsync(ItemEditViewModel model)
        {
            try
            {
                var item = await _context.Items.FindAsync(model.ItemId);
                if (item == null) return false;
                var oldItem = item;
                var oldValues = oldItem;
               // var oldValues = new { ItemName = item.ItemName, ItemCode = item.ItemCode, Category = item.Category, Unit = item.Unit, ReorderLevel = item.ReorderLevel };

                item.ItemName = model.ItemName.Trim();
                item.ItemCode = model.ItemCode.Trim().ToUpper();
                item.Category = model.Category;
                item.Unit = model.Unit;
                item.ItemType = model.ItemType;
                item.ReorderLevel = model.ReorderLevel;
                item.Description = model.Description?.Trim();
                item.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                // Log successful item update
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "UPDATE",
                    tableName: "Items",
                    recordId: item.ItemId.ToString(),
                    oldValues: oldValues,
                    newValues: model,
                    customMessage: $"Item updated: {item.ItemName}"
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteItemAsync(Guid itemId)
        {
            try
            {
                var item = await _context.Items.FindAsync(itemId);
                if (item == null) return false;

                item.IsActive = false;
                await _context.SaveChangesAsync();

                // Log successful item deletion
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "DELETE",
                    tableName: "Items",
                    recordId: itemId.ToString(),
                    customMessage: $"Item soft deleted: {item.ItemName}"
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsItemCodeUniqueAsync(string itemCode, Guid? excludeItemId = null)
        {
            var query = _context.Items.Where(i => i.ItemCode.ToUpper() == itemCode.ToUpper() && i.IsActive);

            if (excludeItemId.HasValue)
                query = query.Where(i => i.ItemId != excludeItemId.Value);

            return !await query.AnyAsync();
        }

        #endregion




        #region Stock Management

        public async Task<bool> ReceiveStockAsync(StockReceiveViewModel model, string userId)
        {
            try
            {
                var stockRecord = new StockRecord
                {
                    StockRecordId = Guid.NewGuid(),
                    ItemId = model.ItemId,
                    StockDate = model.StockDate,
                    QuantityReceived = model.QuantityReceived,
                    UnitCost = model.UnitCost,
                    TotalCost = model.QuantityReceived * model.UnitCost,
                    ExpiryDate = model.ExpiryDate,
                    BatchNumber = model.BatchNumber?.Trim(),
                    //   SupplierId = model.SupplierId,
                    Notes = model.Notes?.Trim(),
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StockRecords.Add(stockRecord);
                await _context.SaveChangesAsync();

                // Log successful stock receive
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "STOCK_RECEIVE",
                    tableName: "StockRecords",
                    recordId: stockRecord.StockRecordId.ToString(),
                    newValues: new { StockRecordId = stockRecord.StockRecordId, ItemId = model.ItemId, Quantity = model.QuantityReceived },
                    customMessage: $"Stock received: {model.QuantityReceived} units of item {model.ItemId}"
                );

                return true;
            }
            catch
            {
                return false;
            }
        }



        //public async Task<bool> IssueStockAsync(StockIssueViewModel model, string userId)
        //{
        //    try
        //    {
        //        // Check if sufficient stock available
        //        var currentStock = await GetCurrentStockAsync(model.ItemId);
        //        if (currentStock < model.QuantityUsed)
        //            return false;

        //        var usageRecord = new UsageRecord
        //        {
        //            UsageRecordId = Guid.NewGuid(),
        //            ItemId = model.ItemId,
        //            UsageDate = model.UsageDate,
        //            QuantityUsed = model.QuantityUsed,
        //            UsageReason = model.UsageReason,
        //            PatientId = model.PatientId,
        //            ProcedureId = model.ProcedureId,
        //            Notes = model.Notes?.Trim(),
        //            CreatedById = userId,
        //            CreatedAt = DateTime.UtcNow,
        //            IsActive = true
        //        };

        //        _context.UsageRecords.Add(usageRecord);
        //        await _context.SaveChangesAsync();
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public async Task<bool> IssueStockAsync(StockIssueViewModel model, string userId)
        {
            try
            {
                // Check if sufficient stock available
                var currentStock = await GetCurrentStockAsync(model.ItemId);
                if (currentStock < model.QuantityUsed)
                    return false;

                // Validate PatientId if provided
                Guid? validatedPatientId = null;
                if (model.PatientId.HasValue && model.PatientId.Value != Guid.Empty)
                {
                    var patientExists = await _context.Patients
                        .AnyAsync(p => p.PatientId == model.PatientId.Value && p.IsActive);

                    if (patientExists)
                    {
                        validatedPatientId = model.PatientId.Value;
                    }
                    // If PatientId provided but doesn't exist, still continue but don't assign invalid ID
                }

                // Validate ProcedureId if provided
                Guid? validatedProcedureId = null;
                if (model.ProcedureId.HasValue && model.ProcedureId.Value != Guid.Empty)
                {
                    var procedureExists = await _context.Procedures
                        .AnyAsync(p => p.ProcedureId == model.ProcedureId.Value);

                    if (procedureExists)
                    {
                        validatedProcedureId = model.ProcedureId.Value;
                    }
                }

                var usageRecord = new UsageRecord
                {
                    UsageRecordId = Guid.NewGuid(),
                    ItemId = model.ItemId,
                    UsageDate = model.UsageDate,
                    QuantityUsed = model.QuantityUsed,
                    UsageReason = model.UsageReason?.Trim(),
                    PatientId = validatedPatientId, // Only assign if valid
                    ProcedureId = validatedProcedureId, // Only assign if valid
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UsageRecords.Add(usageRecord);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception if you have logging
                // _logger.LogError(ex, "Error issuing stock for ItemId: {ItemId}", model.ItemId);
                return false;
            }
        }

        public async Task<int> GetCurrentStockAsync(Guid itemId)
        {
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .SumAsync(sr => sr.QuantityReceived);

            var totalUsed = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .SumAsync(ur => ur.QuantityUsed);

            return totalReceived - totalUsed;
        }

        public async Task<IEnumerable<StockRecord>> GetStockHistoryAsync(Guid itemId)
        {
            return await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .Include(sr => sr.CreatedBy)
                .OrderByDescending(sr => sr.StockDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<UsageRecord>> GetUsageHistoryAsync(Guid itemId)
        {
            return await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .Include(ur => ur.Patient)
                .Include(ur => ur.CreatedBy)
                .OrderByDescending(ur => ur.UsageDate)
                .ToListAsync();
        }

        #endregion

        #region Reports & Analytics

        //public async Task<InventoryDashboardViewModel> GetDashboardDataAsync()
        //{
        //    var totalItems = await _context.Items.CountAsync(i => i.IsActive);


        //    var lowStockItems = await GetLowStockItemsAsync();
        //    var lowStockCount = lowStockItems.Count();

        //    var totalValue = await _context.StockRecords
        //        .Where(sr => sr.IsActive)
        //        .SumAsync(sr => sr.TotalCost);

        //    var recentMovements = await GetRecentMovementsAsync();

        //    return new InventoryDashboardViewModel
        //    {
        //        TotalItems = totalItems,
        //        LowStockItems = lowStockCount,

        //        TotalInventoryValue = totalValue,
        //        LowStockAlerts = lowStockItems.ToList(),
        //        RecentMovements = recentMovements.ToList()
        //    };
        //}


        public async Task<InventoryDashboardViewModel> GetDashboardDataAsync()
        {
            var totalItems = await _context.Items.CountAsync(i => i.IsActive);
            var TotalIprobes = await _context.Items.CountAsync(i => i.IsActive && i.ItemType == "Probe");
            var activeItems = await _context.Items.CountAsync(i => i.IsActive);
            var inactiveItems = await _context.Items.CountAsync(i => !i.IsActive);

            var totalCategories = await _context.Items
                .Where(i => i.IsActive)
                .Select(i => i.Category)
                .Distinct()
                .CountAsync();

            var lowStockItems = await GetLowStockItemsAsync2();
            var lowStockCount = lowStockItems.Count();

            // Calculate out of stock items
            var outOfStockCount = lowStockItems.Count(item => item.CurrentStock <= 0);

       
             var totalValue = await _context.StockRecords
                 .Where(sr => sr.IsActive)
                 .SumAsync(sr => sr.TotalCost);

            var recentMovements = await GetRecentMovementsAsync2();

            // Calculate Current Stock and Used Stock
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .SumAsync(sr => sr.QuantityReceived);

            var totalReceivedprobe = await _context.StockRecords
              .Where(sr => sr.IsActive && sr.Item.ItemType == "Probe")
              .SumAsync(sr => sr.QuantityReceived);

            var totalUsedFromUsageRecords = await _context.UsageRecords
                .Where(ur => ur.IsActive)
                .SumAsync(ur => ur.QuantityUsed);

            var totalUsedprobes = await _context.UsageRecords
               .Where(ur => ur.IsActive && ur.Item.ItemType == "Probe")
               .SumAsync(ur => ur.QuantityUsed);

            var totalUsedFromProcedures = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive)
                .SumAsync(piu => piu.QuantityUsed);

            var totalUsed = totalUsedFromUsageRecords + totalUsedFromProcedures;
          
            var currentStock = totalReceived - totalUsed;
            var currentStockprobe = totalReceivedprobe - totalUsedprobes;

            // Calculate Specific Item Types Stock
            // Total Probes (ItemType = "Probe")
            var probeItemIds = await _context.Items
                .Where(i => i.IsActive && i.ItemType == "Probe")
                .Select(i => i.ItemId)
                .ToListAsync();

            int totalProbes = 0;
            foreach (var itemId in probeItemIds)
            {
                totalProbes += await GetCurrentStockAsync(itemId);
            }

            // Sterile Cover (ItemName contains "Sterile" or "Cover")
            var sterileCoverItemIds = await _context.Items
                .Where(i => i.IsActive &&
                       (i.ItemName.ToLower().Contains("sterile") ||
                        i.ItemName.ToLower().Contains("cover")))
                .Select(i => i.ItemId)
                .ToListAsync();

            int sterileCover = 0;
            foreach (var itemId in sterileCoverItemIds)
            {
                sterileCover += await GetCurrentStockAsync(itemId);
            }

            // Ethanol
            var ethanolItemIds = await _context.Items
                .Where(i => i.IsActive && i.ItemName.ToLower().Contains("ethanol"))
                .Select(i => i.ItemId)
                .ToListAsync();

            int ethanol = 0;
            foreach (var itemId in ethanolItemIds)
            {
                ethanol += await GetCurrentStockAsync(itemId);
            }

            // Liquid Nitrogen
            var liquidNitrogenItemIds = await _context.Items
                .Where(i => i.IsActive &&
                       (i.ItemName.ToLower().Contains("liquid nitrogen") ||
                        i.ItemName.ToLower().Contains("nitrogen")))
                .Select(i => i.ItemId)
                .ToListAsync();

            int liquidNitrogen = 0;
            foreach (var itemId in liquidNitrogenItemIds)
            {
                liquidNitrogen += await GetCurrentStockAsync(itemId);
            }

            return new InventoryDashboardViewModel
            {
                TotalItems = totalItems,
                TotalIprobes = TotalIprobes,
                ActiveItems = activeItems,
                InactiveItems = inactiveItems,
                TotalCategories = totalCategories,
                LowStockItems = lowStockCount,
                OutOfStockItems = outOfStockCount,
                 TotalInventoryValue = totalValue,  // REMOVED
                CurrentStock = currentStock,
                currentStockprobe = currentStockprobe,
                UsedStock = totalUsedprobes,
               // UsedStock = totalUsed,

                // NEW: Specific Item Type Metrics
                TotalProbes = totalProbes,
                SterileCover = sterileCover,
                Ethanol = ethanol,
                LiquidNitrogen = liquidNitrogen,

                LowStockAlerts = lowStockItems.ToList(),
                RecentMovements = recentMovements.ToList()
            };
        }

        // Enhanced GetRecentMovementsAsync method
        private async Task<IEnumerable<RecentStockMovementViewModel>> GetRecentMovementsAsync2()
        {
            // Get recent stock receipts (IN movements)
            var recentIn = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .Include(sr => sr.Item)
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(10)
                .Select(sr => new RecentStockMovementViewModel
                {
                    Date = sr.StockDate,
                    ItemName = sr.Item.ItemName,
                    MovementType = "IN",
                    Quantity = sr.QuantityReceived,
                    Unit = sr.Item.Unit,
                    Reason = "Stock Received"
                })
                .ToListAsync();

            // Get recent stock issues (OUT movements)
            var recentOut = await _context.UsageRecords
                .Where(ur => ur.IsActive)
                .Include(ur => ur.Item)
                .OrderByDescending(ur => ur.CreatedAt)
                .Take(10)
                .Select(ur => new RecentStockMovementViewModel
                {
                    Date = ur.UsageDate,
                    ItemName = ur.Item.ItemName,
                    MovementType = "OUT",
                    Quantity = ur.QuantityUsed,
                    Unit = ur.Item.Unit,
                    Reason = ur.UsageReason
                })
                .ToListAsync();

            // Combine and sort all movements
            return recentIn.Concat(recentOut)
                .OrderByDescending(r => r.Date)
                .Take(15);
        }

        // Enhanced GetLowStockItemsAsync method
        public async Task<IEnumerable<LowStockItemViewModel>> GetLowStockItemsAsync2()
        {
            var items = await _context.Items
                .Where(i => i.IsActive && i.ReorderLevel > 0)
                .ToListAsync();

            var lowStockItems = new List<LowStockItemViewModel>();

            foreach (var item in items)
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);
                if (currentStock <= item.ReorderLevel)
                {
                    lowStockItems.Add(new LowStockItemViewModel
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode,
                        CurrentStock = currentStock,
                        ReorderLevel = item.ReorderLevel,
                        Unit = item.Unit
                    });
                }
            }

            return lowStockItems.OrderBy(l => l.CurrentStock).ThenBy(l => l.ItemName);
        }


        public async Task<IEnumerable<ItemStockReportViewModel>> GetStockReportAsync()
        {
            var items = await _context.Items
                .Where(i => i.IsActive)
                .ToListAsync();

            var reportItems = new List<ItemStockReportViewModel>();

            foreach (var item in items)
            {
                //var currentStock = await GetCurrentStockAsync(item.ItemId);
                //var totalValue = await GetItemTotalValueAsync(item.ItemId);
                //var lastReceived = await GetLastReceivedDateAsync(item.ItemId);
                //var lastIssued = await GetLastIssuedDateAsync(item.ItemId);

                reportItems.Add(new ItemStockReportViewModel
                {
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    ItemCode = item.ItemCode,
                    Category = item.Category,
                    Unit = item.Unit,
                    // CurrentStock = currentStock,
                    ReorderLevel = item.ReorderLevel,
                    //  TotalValue = totalValue,
                    //   LastReceived = lastReceived,
                    //   LastIssued = lastIssued
                });
            }

            return reportItems.OrderBy(r => r.ItemName);
        }

        public async Task<IEnumerable<LowStockItemViewModel>> GetLowStockItemsAsync()
        {
            var items = await _context.Items
                .Where(i => i.IsActive && i.ReorderLevel > 0)
                .ToListAsync();

            var lowStockItems = new List<LowStockItemViewModel>();

            foreach (var item in items)
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);
                if (currentStock <= item.ReorderLevel)
                {
                    lowStockItems.Add(new LowStockItemViewModel
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode,
                        CurrentStock = currentStock,
                        ReorderLevel = item.ReorderLevel,
                        Unit = item.Unit
                    });
                }
            }

            return lowStockItems.OrderBy(l => l.ItemName);
        }

        #endregion

        #region Utility Methods

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _context.Items
                .Where(i => i.IsActive)
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetUnitsAsync()
        {
            return await _context.Items
                .Where(i => i.IsActive)
                .Select(i => i.Unit)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
        }

        #endregion

        #region Private Helper Methods

        private async Task<IEnumerable<RecentStockMovementViewModel>> GetRecentMovementsAsync()
        {
            var recentIn = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .Include(sr => sr.Item)
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(5)
                .Select(sr => new RecentStockMovementViewModel
                {
                    Date = sr.StockDate,
                    ItemName = sr.Item.ItemName,
                    MovementType = "IN",
                    Quantity = sr.QuantityReceived,
                    Unit = sr.Item.Unit,
                    Reason = "Stock Received"
                })
                .ToListAsync();

            var recentOut = await _context.UsageRecords
                .Where(ur => ur.IsActive)
                .Include(ur => ur.Item)
                .OrderByDescending(ur => ur.CreatedAt)
                .Take(5)
                .Select(ur => new RecentStockMovementViewModel
                {
                    Date = ur.UsageDate,
                    ItemName = ur.Item.ItemName,
                    MovementType = "OUT",
                    Quantity = ur.QuantityUsed,
                    Unit = ur.Item.Unit,
                    Reason = ur.UsageReason
                })
                .ToListAsync();

            return recentIn.Concat(recentOut)
                .OrderByDescending(r => r.Date)
                .Take(10);
        }

        private async Task<decimal> GetItemTotalValueAsync(Guid itemId)
        {
            var currentStock = await GetCurrentStockAsync(itemId);
            if (currentStock <= 0) return 0;

            // Calculate weighted average cost
            var stockRecords = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .OrderByDescending(sr => sr.StockDate)
                .ToListAsync();

            if (!stockRecords.Any()) return 0;

            var totalCost = stockRecords.Sum(sr => sr.TotalCost);
            var totalQuantity = stockRecords.Sum(sr => sr.QuantityReceived);

            if (totalQuantity == 0) return 0;

            var avgCost = totalCost / totalQuantity;
            return currentStock * avgCost;
        }

        private async Task<DateTime> GetLastReceivedDateAsync(Guid itemId)
        {
            var lastRecord = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .OrderByDescending(sr => sr.StockDate)
                .FirstOrDefaultAsync();

            return lastRecord?.StockDate ?? DateTime.MinValue;
        }

        private async Task<DateTime> GetLastIssuedDateAsync(Guid itemId)
        {
            var lastRecord = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .OrderByDescending(ur => ur.UsageDate)
                .FirstOrDefaultAsync();

            return lastRecord?.UsageDate ?? DateTime.MinValue;
        }

        #endregion


        #region Excel

        public async Task<ExcelUploadViewModels> UploadExcelDataAsync(IFormFile excelFile, string userId)
        {
            var result = new ExcelUploadViewModels
            {
                SuccessfulItems = new List<string>(),
                FailedItems = new List<ExcelUploadErrorViewModel>(),
                TotalRowsProcessed = 0,
                SuccessCount = 0,
                FailureCount = 0
            };

            try
            {
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row

                        foreach (var row in rows)
                        {
                            result.TotalRowsProcessed++;
                            var rowNumber = row.RowNumber();

                            try
                            {
                                // Excel columns: Sr No., ItemName, ItemCode, category, unit, QuantityReceived
                                var srNo = row.Cell(1).GetValue<string>();
                                var itemName = row.Cell(2).GetValue<string>()?.Trim();
                                var itemCode = row.Cell(3).GetValue<string>()?.Trim();
                                var category = row.Cell(4).GetValue<string>()?.Trim();
                                var unit = row.Cell(5).GetValue<string>()?.Trim();
                                var quantityReceivedStr = row.Cell(6).GetValue<string>()?.Trim();

                                // Validation
                                var validationErrors = new List<string>();

                                if (string.IsNullOrWhiteSpace(itemName))
                                    validationErrors.Add("Item Name is required");

                                if (string.IsNullOrWhiteSpace(itemCode))
                                    validationErrors.Add("Item Code is required");

                                if (string.IsNullOrWhiteSpace(category))
                                    validationErrors.Add("Category is required");

                                if (string.IsNullOrWhiteSpace(unit))
                                    validationErrors.Add("Unit is required");

                                // Parse quantity
                                int quantityReceived = 0;
                                bool hasQuantity = false;
                                if (!string.IsNullOrWhiteSpace(quantityReceivedStr))
                                {
                                    if (!int.TryParse(quantityReceivedStr, out quantityReceived) || quantityReceived < 0)
                                    {
                                        validationErrors.Add("Quantity must be a valid positive number");
                                    }
                                    else
                                    {
                                        hasQuantity = true;
                                    }
                                }

                                if (validationErrors.Any())
                                {
                                    result.FailedItems.Add(new ExcelUploadErrorViewModel
                                    {
                                        RowNumber = rowNumber,
                                        SerialNumber = srNo,
                                        ItemCode = itemCode ?? "N/A",
                                        ItemName = itemName ?? "N/A",
                                        Errors = validationErrors
                                    });
                                    result.FailureCount++;
                                    continue;
                                }

                                // Check if item already exists by ItemCode
                                var existingItem = await _context.Items
                                    .FirstOrDefaultAsync(i => i.ItemCode == itemCode && i.IsActive);

                                Guid itemId;

                                if (existingItem != null)
                                {
                                    // Item exists, use existing ItemId
                                    itemId = existingItem.ItemId;
                                }
                                else
                                {
                                    // Create new item
                                    var newItem = new Item
                                    {
                                        ItemId = Guid.NewGuid(),
                                        ItemName = itemName,
                                        ItemCode = itemCode,
                                        Category = category,
                                        Unit = unit,
                                        ItemType = "Probe",
                                        ReorderLevel = 0,
                                        Description = "Added via Excel Upload",
                                        CreatedById = userId,
                                        CreatedAt = DateTime.UtcNow,
                                        IsActive = true
                                    };

                                    _context.Items.Add(newItem);
                                    await _context.SaveChangesAsync();
                                    itemId = newItem.ItemId;
                                }

                                // Add stock record only if quantity is provided
                                if (hasQuantity && quantityReceived > 0)
                                {
                                    var stockRecord = new StockRecord
                                    {
                                        StockRecordId = Guid.NewGuid(),
                                        ItemId = itemId,
                                        StockDate = DateTime.UtcNow,
                                        QuantityReceived = quantityReceived,
                                        UnitCost = 0,
                                        TotalCost = 0,
                                        BatchNumber = "N/A",
                                        Notes = "Stock added via Excel Upload",
                                        CreatedById = userId,
                                        CreatedAt = DateTime.UtcNow,
                                        IsActive = true
                                    };

                                    _context.StockRecords.Add(stockRecord);
                                    await _context.SaveChangesAsync();

                                    result.SuccessfulItems.Add($"Row {rowNumber} (Sr No: {srNo}): {itemName} - Item and Stock added successfully");
                                }
                                else
                                {
                                    result.SuccessfulItems.Add($"Row {rowNumber} (Sr No: {srNo}): {itemName} - Item added successfully (No stock quantity provided)");
                                }

                                result.SuccessCount++;
                            }
                            catch (Exception ex)
                            {
                                result.FailedItems.Add(new ExcelUploadErrorViewModel
                                {
                                    RowNumber = rowNumber,
                                    SerialNumber = row.Cell(1).GetValue<string>(),
                                    ItemCode = row.Cell(3).GetValue<string>(),
                                    ItemName = row.Cell(2).GetValue<string>(),
                                    Errors = new List<string> { $"Error: {ex.InnerException?.Message}" }
                                });
                                result.FailureCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new ExcelUploadErrorViewModel
                {
                    RowNumber = 0,
                    SerialNumber = "N/A",
                    ItemCode = "N/A",
                    ItemName = "N/A",
                    Errors = new List<string> { $"File processing error: {ex.Message}" }
                });
            }

            return result;
        }


        #endregion


    }
}
