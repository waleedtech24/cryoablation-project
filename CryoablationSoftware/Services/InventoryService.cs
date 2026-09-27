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
        private readonly ICurrentSiteService _currentSiteService;

        public InventoryService(ApplicationDbContext context, IAuditLogService auditLogService, ICurrentSiteService currentSiteService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _currentSiteService = currentSiteService;
        }



        #region Grouped Items

        public async Task<IEnumerable<ItemGroupViewModel>> GetGroupedItemsAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var query = _context.Items
                .Include(i => i.CreatedBy)
                .Where(i => i.IsActive);

            if (siteId.HasValue)
                query = query.Where(i => i.SiteId == siteId);

            var items = await query.ToListAsync();

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

                var earliestExpiry = await _context.StockRecords
            .Where(sr => sr.ItemId == item.ItemId && sr.IsActive && sr.ExpiryDate != null)
            .OrderBy(sr => sr.ExpiryDate)
            .Select(sr => sr.ExpiryDate)
            .FirstOrDefaultAsync();

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
                    CreatedAt = item.CreatedAt,
                    EarliestExpiry = earliestExpiry
                });
            }

            return variants;
        }

        #endregion

        #region Item Management

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var query = _context.Items
                .Include(i => i.CreatedBy)
                .OrderBy(i => i.ItemName)
                .AsQueryable();

            if (siteId.HasValue)
                query = query.Where(i => i.SiteId == siteId);

            return await query.ToListAsync();
        }

        private async Task<List<ExpiringStockViewModel>> GetExpiringStockAsync(int? siteId)
        {
            var today = DateTime.Today;
            var cutoff = today.AddDays(90); // 90 din k andar expiry

            var records = await _context.StockRecords
                .Where(sr => sr.IsActive
                    && sr.ExpiryDate != null
                    && sr.ExpiryDate.Value <= cutoff
                    && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .Include(sr => sr.Item)
                .OrderBy(sr => sr.ExpiryDate)
                .Select(sr => new ExpiringStockViewModel
                {
                    ItemName = sr.Item.ItemName,
                    ItemCode = sr.Item.ItemCode,
                    Unit = sr.Item.Unit,
                    QuantityReceived = sr.QuantityReceived,
                    ExpiryDate = sr.ExpiryDate.Value,
                    DaysUntilExpiry = (int)(sr.ExpiryDate.Value - today).TotalDays
                })
                .ToListAsync();

            return records;
        }

        // for probe inventory 

        public async Task<ProbeDashboardViewModel> GetProbeDashboardAsync()
        {
            // Saare active probe items
            var probeItems = await _context.Items
                .Where(i => i.IsActive && i.ItemType == "Probe")
                .Select(i => new { i.ItemId, i.ItemName, i.Unit })
                .ToListAsync();

            var probeItemIds = probeItems.Select(i => i.ItemId).ToList();

            // Per-item received
            var receivedPerItem = await _context.StockRecords
                .Where(sr => sr.IsActive && probeItemIds.Contains(sr.ItemId))
                .GroupBy(sr => sr.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalReceived = g.Sum(x => x.QuantityReceived)
                })
                .ToListAsync();

            // Per-item used (UsageRecords)
            var usedPerItem = await _context.UsageRecords
                .Where(ur => ur.IsActive && probeItemIds.Contains(ur.ItemId))
                .GroupBy(ur => ur.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalUsed = g.Sum(x => x.QuantityUsed)
                })
                .ToListAsync();

            // ItemName se group — same naam ke variants ek row mein
            var breakdown = probeItems
                .GroupBy(i => i.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var idsInGroup = g.Select(x => x.ItemId).ToList();

                    var totalReceived = receivedPerItem
                        .Where(r => idsInGroup.Contains(r.ItemId))
                        .Sum(r => r.TotalReceived);

                    var totalUsed = usedPerItem
                        .Where(u => idsInGroup.Contains(u.ItemId))
                        .Sum(u => u.TotalUsed);

                    return new ProbeBreakdownViewModel
                    {
                        ProbeName = g.Key,
                        Unit = g.First().Unit,
                        TotalReceived = totalReceived,
                        TotalUsed = totalUsed
                    };
                })
                .OrderBy(p => p.ProbeName)
                .ToList();

            return new ProbeDashboardViewModel
            {
                TotalProbesReceived = breakdown.Sum(p => p.TotalReceived),
                TotalProbesUsed = breakdown.Sum(p => p.TotalUsed),
                TotalProbesAvailable = breakdown.Sum(p => p.Available),
                ProbeBreakdown = breakdown
            };
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
                var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

                var item = new Item
                {
                    ItemId = Guid.NewGuid(),
                    ItemName = model.ItemName.Trim(),
                    ItemCode = model.ItemCode.Trim().ToUpper(),
                    Category = model.Category,
                    Unit = model.Unit,
                    ItemType = model.ItemType,
                    ReorderLevel = model.ReorderLevel,
                    Description = model.Description?.Trim(),
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    SiteId = siteId  // MULTI-SITE
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
                    PatientId = validatedPatientId,
                    ProcedureId = validatedProcedureId,
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

        public async Task<InventoryDashboardViewModel> GetDashboardDataAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var totalItems = await _context.Items.CountAsync(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId));
            var TotalIprobes = await _context.Items.CountAsync(i => i.IsActive && i.ItemType == "Probe" && (!siteId.HasValue || i.SiteId == siteId));
            var activeItems = totalItems;
            var inactiveItems = await _context.Items.CountAsync(i => !i.IsActive && (!siteId.HasValue || i.SiteId == siteId));

            var totalCategories = await _context.Items
                .Where(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId))
                .Select(i => i.Category)
                .Distinct()
                .CountAsync();

            var lowStockItems = await GetLowStockItemsAsync2();
            var lowStockCount = lowStockItems.Count();

            // Calculate out of stock items
            var outOfStockCount = lowStockItems.Count(item => item.CurrentStock <= 0);

            var totalValue = await _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .SumAsync(sr => sr.TotalCost);

            var recentMovements = await GetRecentMovementsAsync2();

            // Calculate Current Stock and Used Stock
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .SumAsync(sr => sr.QuantityReceived);

            var totalReceivedprobe = await _context.StockRecords
              .Where(sr => sr.IsActive && sr.Item.ItemType == "Probe" && (!siteId.HasValue || sr.Item.SiteId == siteId))
              .SumAsync(sr => sr.QuantityReceived);

            var totalUsedFromUsageRecords = await _context.UsageRecords
                .Where(ur => ur.IsActive && (!siteId.HasValue || ur.Item.SiteId == siteId))
                .SumAsync(ur => ur.QuantityUsed);

            var totalUsedprobes = await _context.UsageRecords
               .Where(ur => ur.IsActive && ur.Item.ItemType == "Probe" && (!siteId.HasValue || ur.Item.SiteId == siteId))
               .SumAsync(ur => ur.QuantityUsed);

            var totalUsedFromProcedures = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive && (!siteId.HasValue || piu.Item.SiteId == siteId))
                .SumAsync(piu => piu.QuantityUsed);

            var totalUsed = totalUsedFromUsageRecords + totalUsedFromProcedures;

            var currentStock = totalReceived - totalUsed;
            var currentStockprobe = totalReceivedprobe - totalUsedprobes;

            // Calculate Specific Item Types Stock - all filtered by site
            var probeItemIds = await _context.Items
                .Where(i => i.IsActive && i.ItemType == "Probe" && (!siteId.HasValue || i.SiteId == siteId))
                .Select(i => i.ItemId)
                .ToListAsync();

            int totalProbes = 0;
            foreach (var itemId in probeItemIds)
            {
                totalProbes += await GetCurrentStockAsync(itemId);
            }

            var sterileCoverItemIds = await _context.Items
                .Where(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId) &&
                       (i.ItemName.ToLower().Contains("sterile") ||
                        i.ItemName.ToLower().Contains("cover")))
                .Select(i => i.ItemId)
                .ToListAsync();

            int sterileCover = 0;
            foreach (var itemId in sterileCoverItemIds)
            {
                sterileCover += await GetCurrentStockAsync(itemId);
            }

            var ethanolItemIds = await _context.Items
                .Where(i => i.IsActive && i.ItemName.ToLower().Contains("ethanol") && (!siteId.HasValue || i.SiteId == siteId))
                .Select(i => i.ItemId)
                .ToListAsync();

            int ethanol = 0;
            foreach (var itemId in ethanolItemIds)
            {
                ethanol += await GetCurrentStockAsync(itemId);
            }

            var liquidNitrogenItemIds = await _context.Items
                .Where(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId) &&
                       (i.ItemName.ToLower().Contains("liquid nitrogen") ||
                        i.ItemName.ToLower().Contains("nitrogen")))
                .Select(i => i.ItemId)
                .ToListAsync();

            int liquidNitrogen = 0;
            foreach (var itemId in liquidNitrogenItemIds)
            {
                liquidNitrogen += await GetCurrentStockAsync(itemId);
            }
            var expiringItems = await GetExpiringStockAsync(siteId.HasValue ? (int?)siteId.Value : null);


            return new InventoryDashboardViewModel
            {
                TotalItems = totalItems,
                TotalIprobes = TotalIprobes,
                ActiveItems = activeItems,
                InactiveItems = inactiveItems,
                TotalCategories = totalCategories,
                LowStockItems = lowStockCount,
                OutOfStockItems = outOfStockCount,
                TotalInventoryValue = totalValue,
                CurrentStock = currentStock,
                currentStockprobe = currentStockprobe,
                UsedStock = totalUsedprobes,

                // NEW: Specific Item Type Metrics
                TotalProbes = totalProbes,
                SterileCover = sterileCover,
                Ethanol = ethanol,
                LiquidNitrogen = liquidNitrogen,
                ExpiringItems = expiringItems,
                LowStockAlerts = lowStockItems.ToList(),
                RecentMovements = recentMovements.ToList()
            };
        }

        // Enhanced GetRecentMovementsAsync method
        private async Task<IEnumerable<RecentStockMovementViewModel>> GetRecentMovementsAsync2()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            // Get recent stock receipts (IN movements)
            var recentIn = await _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
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
                .Where(ur => ur.IsActive && (!siteId.HasValue || ur.Item.SiteId == siteId))
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
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var items = await _context.Items
                .Where(i => i.IsActive && i.ReorderLevel > 0 && (!siteId.HasValue || i.SiteId == siteId))
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
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var items = await _context.Items
                .Where(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId))
                .ToListAsync();

            var reportItems = new List<ItemStockReportViewModel>();

            foreach (var item in items)
            {
                reportItems.Add(new ItemStockReportViewModel
                {
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    ItemCode = item.ItemCode,
                    Category = item.Category,
                    Unit = item.Unit,
                    ReorderLevel = item.ReorderLevel,
                });
            }

            return reportItems.OrderBy(r => r.ItemName);
        }

        public async Task<IEnumerable<LowStockItemViewModel>> GetLowStockItemsAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();

            var items = await _context.Items
                .Where(i => i.IsActive && i.ReorderLevel > 0 && (!siteId.HasValue || i.SiteId == siteId))
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

        public async Task<ExcelUploadViewModels> UploadExcelDataAsync(IFormFile excelFile, string userId, int? siteId)
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
                                        SiteId = siteId,
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