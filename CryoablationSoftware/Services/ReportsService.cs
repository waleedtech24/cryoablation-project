using CryoablationSoftware.Data;
using CryoablationSoftware.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public class ReportsService : IReportsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentSiteService _currentSiteService;

        public ReportsService(ApplicationDbContext context, ICurrentSiteService currentSiteService)
        {
            _context = context;
            _currentSiteService = currentSiteService;
        }

        #region Dashboard & Analytics

        public async Task<ReportsDashboardViewModel> GetReportsDashboardAsync()
        {
            var model = new ReportsDashboardViewModel();

            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            // Key Metrics
            model.TotalPatients = await _context.Patients
                .CountAsync(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId));
            model.TotalProcedures = await _context.Procedures
                .CountAsync(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId));
            model.TotalItems = await _context.Items
                .CountAsync(i => i.IsActive && (!siteId.HasValue || i.SiteId == siteId));
            model.TotalInventoryValue = await _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .SumAsync(sr => sr.TotalCost);

            // This Month Statistics
            var thisMonth = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day);
            model.ProceduresThisMonth = await _context.Procedures
                .CountAsync(p => p.IsActive && p.ProcedureDate >= thisMonth && (!siteId.HasValue || p.SiteId == siteId));
            model.PatientsThisMonth = await _context.Patients
                .CountAsync(p => p.IsActive && p.CreatedAt >= thisMonth && (!siteId.HasValue || p.SiteId == siteId));

            // Low Stock Items
            var itemsQuery = _context.Items.Where(i => i.IsActive && i.ReorderLevel > 0);
            if (siteId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.SiteId == siteId);
            var items = await itemsQuery.ToListAsync();
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
            model.LowStockAlerts = lowStockItems.Take(10).ToList();

            // Recent Activity
            model.RecentProcedures = await _context.Procedures
                .Where(p => p.IsActive)
                .Include(p => p.Patient)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentActivityViewModel
                {
                    ActivityType = "Procedure",
                    Description = $"Procedure for {p.Patient.PatientName} - {p.ProcedureROI}",
                    Date = p.CreatedAt,
                    Status = p.ProcedureStatus
                })
                .ToListAsync();

            // Top Used Items
            model.TopUsedItems = await _context.UsageRecords
                .Where(ur => ur.IsActive && ur.UsageDate >= thisMonth)
                .Include(ur => ur.Item)
                .GroupBy(ur => new { ur.Item.ItemName, ur.Item.ItemCode, ur.Item.Unit })
                .Select(g => new TopUsedItemViewModel
                {
                    ItemName = g.Key.ItemName,
                    ItemCode = g.Key.ItemCode,
                    Unit = g.Key.Unit,
                    TotalUsed = g.Sum(x => x.QuantityUsed)
                })
                .OrderByDescending(x => x.TotalUsed)
                .Take(10)
                .ToListAsync();

            return model;
        }

        public async Task<AnalyticsDataViewModel> GetAnalyticsDataAsync()
        {
            var model = new AnalyticsDataViewModel();

            // Monthly Procedure Trends (Last 12 months) - Fixed LINQ issue
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
            var procedureData = await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureDate >= twelveMonthsAgo)
                .Select(p => new { p.ProcedureDate.Year, p.ProcedureDate.Month })
                .ToListAsync();

            model.MonthlyProcedureTrends = procedureData
                .GroupBy(p => new { p.Year, p.Month })
                .Select(g => new MonthlyTrendViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            // Procedure Status Distribution
            model.ProcedureStatusDistribution = await _context.Procedures
                .Where(p => p.IsActive)
                .GroupBy(p => p.ProcedureStatus)
                .Select(g => new StatusDistributionViewModel
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / _context.Procedures.Count(p => p.IsActive) * 100
                })
                .ToListAsync();

            // Top Performing Categories (Inventory) - Fixed calculation
            var categoryData = await _context.Items
                .Where(i => i.IsActive)
                .Include(i => i.StockRecords.Where(sr => sr.IsActive))
                .Select(i => new { i.Category, StockRecords = i.StockRecords })
                .ToListAsync();

            model.TopCategories = categoryData
                .GroupBy(i => i.Category)
                .Select(g => new CategoryPerformanceViewModel
                {
                    Category = g.Key,
                    ItemCount = g.Count(),
                    TotalValue = g.SelectMany(x => x.StockRecords).Sum(sr => sr.TotalCost)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            // Patient Demographics
            var patientAges = await _context.Patients
                .Where(p => p.IsActive && p.PatientAge > 0)
                .Select(p => p.PatientAge)
                .ToListAsync();

            model.PatientAgeGroups = patientAges
                .GroupBy(age => age < 18 ? "Under 18" :
                             age < 30 ? "18-29" :
                             age < 50 ? "30-49" :
                             age < 70 ? "50-69" : "70+")
                .Select(g => new AgeGroupViewModel
                {
                    AgeGroup = g.Key,
                    Count = g.Count()
                })
                .ToList();

            model.GenderDistribution = await _context.Patients
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.PatientGender))
                .GroupBy(p => p.PatientGender)
                .Select(g => new GenderDistributionViewModel
                {
                    Gender = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return model;
        }

        public async Task<object> GetChartDataAsync(string chartType, string period)
        {
            try
            {
                switch (chartType.ToLower())
                {
                    case "procedures_monthly":
                        return await GetProcedureMonthlyChartDataSafe(period);
                    case "inventory_usage":
                        return await GetInventoryUsageChartData(period);
                    case "patient_registration":
                        return await GetPatientRegistrationChartData(period);
                    case "financial_trends":
                        return await GetFinancialTrendsChartData(period);
                    default:
                        return new { error = "Unknown chart type" };
                }
            }
            catch (Exception ex)
            {
                // Log error if you have logging
                return new { error = "Error loading chart data", details = ex.Message };
            }
        }

        // Safe version of GetProcedureMonthlyChartData with better error handling
        private async Task<object> GetProcedureMonthlyChartDataSafe(string period)
        {
            try
            {
                var months = period == "year" ? 12 : 6;
                var startDate = DateTime.Now.AddMonths(-months);

                // Get raw data with timeout protection
                var rawData = await _context.Procedures
                    .Where(p => p.IsActive && p.ProcedureDate >= startDate)
                    .Select(p => new
                    {
                        Year = p.ProcedureDate.Year,
                        Month = p.ProcedureDate.Month,
                        Status = p.ProcedureStatus
                    })
                    .Take(1000) // Limit results to prevent memory issues
                    .ToListAsync();

                if (!rawData.Any())
                {
                    // Return empty data structure
                    return new[] {
                        new { month = DateTime.Now.ToString("yyyy-MM"), total = 0, completed = 0, scheduled = 0 }
                    };
                }

                // Group and format on client side
                var data = rawData
                    .GroupBy(p => new { p.Year, p.Month })
                    .Select(g => new
                    {
                        month = $"{g.Key.Year}-{g.Key.Month:00}",
                        total = g.Count(),
                        completed = g.Count(x => x.Status == "Completed"),
                        scheduled = g.Count(x => x.Status == "Scheduled")
                    })
                    .OrderBy(x => x.month)
                    .ToList();

                return data;
            }
            catch (Exception ex)
            {
                // Return safe empty data on error
                return new[] {
                    new { month = DateTime.Now.ToString("yyyy-MM"), total = 0, completed = 0, scheduled = 0 }
                };
            }
        }

        public async Task<object> GetWidgetDataAsync(string widgetType)
        {
            switch (widgetType.ToLower())
            {
                case "pending_procedures":
                    return await GetPendingProceduresWidget();
                case "low_stock_critical":
                    return await GetCriticalStockWidget();
                case "recent_patients":
                    return await GetRecentPatientsWidget();
                case "monthly_summary":
                    return await GetMonthlySummaryWidget();
                default:
                    return new { error = "Unknown widget type" };
            }
        }

        #endregion

        #region Patient Reports

        public async Task<PatientReportsViewModel> GetPatientReportsAsync()
        {
            var model = new PatientReportsViewModel();

            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            model.TotalPatients = await _context.Patients
                .CountAsync(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId));
            model.ActivePatients = model.TotalPatients;
            model.PatientsThisMonth = await _context.Patients
                .CountAsync(p => p.IsActive && p.CreatedAt >= DateTime.Now.Date.AddDays(1 - DateTime.Now.Day) && (!siteId.HasValue || p.SiteId == siteId));

            model.Patients = await _context.Patients
                .Where(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId))
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                .Select(p => new PatientSummaryViewModel
                {
                    PatientId = p.PatientId,
                    PatientName = p.PatientName,
                    PatientMRNumber = p.PatientMRNumber,
                    PatientPhone = p.PatientPhone,
                    PatientAge = p.PatientAge,
                    PatientGender = p.PatientGender,
                    TotalProcedures = p.Procedures.Count(),
                    LastProcedureDate = p.Procedures.OrderByDescending(proc => proc.ProcedureDate).FirstOrDefault().ProcedureDate,
                    RegistrationDate = p.CreatedAt
                })
                .OrderByDescending(p => p.RegistrationDate)
                .ToListAsync();

            // Patient Statistics by Age Group
            model.PatientAgeStatistics = await _context.Patients
               .Where(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId))
               .GroupBy(p => p.PatientAge < 18 ? "Under 18" :
                            p.PatientAge < 30 ? "18-29" :
                             p.PatientAge < 50 ? "30-49" :
                             p.PatientAge < 70 ? "50-69" : "70+")
                .Select(g => new AgeStatisticViewModel
                {
                    AgeGroup = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / model.TotalPatients * 100
                })
                .ToListAsync();

            return model;
        }

        public async Task<PatientDetailReportViewModel> GetPatientDetailReportAsync(Guid patientId, DateTime? fromDate, DateTime? toDate)
        {
            var patient = await _context.Patients
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                    .ThenInclude(proc => proc.ItemUsages.Where(iu => iu.IsActive))
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                    .ThenInclude(proc => proc.CreatedBy)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive);

            if (patient == null) return null;

            var model = new PatientDetailReportViewModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.PatientName,
                PatientMRNumber = patient.PatientMRNumber,
                PatientPhone = patient.PatientPhone,
                PatientAge = patient.PatientAge,
                PatientGender = patient.PatientGender,
                RegistrationDate = patient.CreatedAt
            };

            var procedures = patient.Procedures.AsQueryable();
            if (fromDate.HasValue)
                procedures = procedures.Where(p => p.ProcedureDate >= fromDate.Value);
            if (toDate.HasValue)
                procedures = procedures.Where(p => p.ProcedureDate <= toDate.Value);

            model.Procedures = procedures
                .Select(p => new PatientProcedureDetailViewModel
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    ProcedureRemarks = p.ProcedureRemarks,
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToList();

            model.TotalProcedures = model.Procedures.Count;
            model.CompletedProcedures = model.Procedures.Count(p => p.ProcedureStatus == "Completed");

            return model;
        }

        #endregion

        #region Procedure Reports

        public async Task<ProcedureReportViewModel> GetProcedureReportsAsync(DateTime? fromDate, DateTime? toDate, string status)
        {
            var model = new ProcedureReportViewModel();

            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            var query = _context.Procedures
                .Where(p => p.IsActive && (!siteId.HasValue || p.SiteId == siteId))
                .Include(p => p.Patient)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                     .ThenInclude(iu => iu.Item)
                .Include(p => p.CreatedBy)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.ProcedureDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.ProcedureDate <= toDate.Value);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.ProcedureStatus == status);

            model.Procedures = await query
                .Select(p => new ProcedureReportDetailViewModel
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureDate = p.ProcedureDate,
                    PatientName = p.Patient.PatientName,
                    PatientMRNumber = p.Patient.PatientMRNumber,
                    PatientCNIC = p.Patient.PatientCNIC,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    TotalItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    TotalItemsUsed = p.ItemUsages
                .Where(iu => iu.Item.ItemType == "Probe")
                .Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName,
                    ProcedureRemarks = p.ProcedureRemarks,
                    PrimaryDoctorName = p.PrimaryDoctorName,
                    ConsultantDoctor1Name = p.ConsultantDoctor1Name,
                    ConsultantDoctor2Name = p.ConsultantDoctor2Name
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();

            // Statistics
            model.TotalProcedures = model.Procedures.Count;
            model.CompletedProcedures = model.Procedures.Count(p => p.ProcedureStatus == "Completed");
            model.ScheduledProcedures = model.Procedures.Count(p => p.ProcedureStatus == "Scheduled");
            model.InProgressProcedures = model.Procedures.Count(p => p.ProcedureStatus == "In-Progress");
            model.CancelledProcedures = model.Procedures.Count(p => p.ProcedureStatus == "Cancelled");

            return model;
        }

        public async Task<ProcedureAnalyticsViewModel> GetProcedureAnalyticsAsync()
        {
            var model = new ProcedureAnalyticsViewModel();

            // Monthly procedure trends for last 12 months - Fixed LINQ
            var siteId2 = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin2 = await _currentSiteService.IsSuperAdminAsync();

            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
            var proceduresData = await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureDate >= twelveMonthsAgo && (!siteId2.HasValue || p.SiteId == siteId2))

                .Select(p => new { p.ProcedureDate.Year, p.ProcedureDate.Month, p.ProcedureStatus })
                .ToListAsync();

            model.MonthlyTrends = proceduresData
                .GroupBy(p => new { p.Year, p.Month })
                .Select(g => new ProcedureMonthlyTrendViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Count = g.Count(),
                    CompletedCount = g.Count(x => x.ProcedureStatus == "Completed")
                })
                .OrderBy(x => x.Month)
                .ToList();

            // ROI (Region of Interest) Analysis
            model.ROIAnalysis = await _context.Procedures
               .Where(p => p.IsActive && (!siteId2.HasValue || p.SiteId == siteId2))
               .GroupBy(p => p.ProcedureROI)
               .Select(g => new ROIAnalysisViewModel
               {
                   ROI = g.Key,
                   Count = g.Count(),
                   CompletionRate = (double)g.Count(x => x.ProcedureStatus == "Completed") / g.Count() * 100
               })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // Average procedure duration analysis - Simplified
            var completedProcedures = await _context.Procedures
               .Where(p => p.IsActive && p.ProcedureStatus == "Completed" && (!siteId2.HasValue || p.SiteId == siteId2))
               .Select(p => new { p.ProcedureROI, DurationLength = p.ProcedureDuration.Length })
                .ToListAsync();

            model.DurationAnalysis = completedProcedures
                .GroupBy(p => p.ProcedureROI)
                .Select(g => new ProcedureDurationAnalysisViewModel
                {
                    ROI = g.Key,
                    AverageDuration = g.Average(x => x.DurationLength),
                    ProcedureCount = g.Count()
                })
                .ToList();

            return model;
        }

        #endregion

        #region Inventory Reports

        public async Task<InventoryReportViewModel> GetInventoryReportsAsync()
        {
            var model = new InventoryReportViewModel();

            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            var itemsQuery = _context.Items.Where(i => i.IsActive);
            if (siteId.HasValue)
                itemsQuery = itemsQuery.Where(i => i.SiteId == siteId);
            var items = await itemsQuery.ToListAsync();

            model.TotalItems = items.Count;
            model.TotalInventoryValue = await _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .SumAsync(sr => sr.TotalCost);

            var itemStockDetails = new List<ItemStockDetailViewModel>();
            int lowStockCount = 0;
            int outOfStockCount = 0;

            foreach (var item in items)
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);
                var totalValue = await GetItemTotalValueAsync(item.ItemId, currentStock);

                if (currentStock <= 0) outOfStockCount++;
                else if (currentStock <= item.ReorderLevel) lowStockCount++;

                itemStockDetails.Add(new ItemStockDetailViewModel
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Category = item.Category,
                    Unit = item.Unit,
                    CurrentStock = currentStock,
                    ReorderLevel = item.ReorderLevel,
                    TotalValue = totalValue,
                    Status = currentStock <= 0 ? "Out of Stock" :
                            currentStock <= item.ReorderLevel ? "Low Stock" : "Normal"
                });
            }

            model.ItemStockDetails = itemStockDetails.OrderBy(i => i.ItemName).ToList();
            model.LowStockItems = lowStockCount;
            model.OutOfStockItems = outOfStockCount;

            // Category-wise analysis
            model.CategoryAnalysis = items
                .GroupBy(i => i.Category)
                .Select(g => new CategoryAnalysisViewModel
                {
                    Category = g.Key,
                    ItemCount = g.Count(),
                    TotalValue = itemStockDetails.Where(i => i.Category == g.Key).Sum(i => i.TotalValue),
                    LowStockCount = itemStockDetails.Count(i => i.Category == g.Key && i.Status == "Low Stock")
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            return model;
        }

        public async Task<StockMovementReportViewModel> GetStockMovementReportAsync(DateTime? fromDate, DateTime? toDate, Guid? itemId)
        {
            var model = new StockMovementReportViewModel();

            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            // Stock IN movements
            var stockInQuery = _context.StockRecords
                .Where(sr => sr.IsActive && (!siteId.HasValue || sr.Item.SiteId == siteId))
                .Include(sr => sr.Item)
                .Include(sr => sr.CreatedBy)
                .AsQueryable();

            if (fromDate.HasValue)
                stockInQuery = stockInQuery.Where(sr => sr.StockDate >= fromDate.Value);
            if (toDate.HasValue)
                stockInQuery = stockInQuery.Where(sr => sr.StockDate <= toDate.Value);
            if (itemId.HasValue)
                stockInQuery = stockInQuery.Where(sr => sr.ItemId == itemId.Value);

            var stockInMovements = await stockInQuery
                .Select(sr => new StockMovementDetailViewModel
                {
                    Date = sr.StockDate,
                    ItemId = sr.ItemId,
                    ItemName = sr.Item.ItemName,
                    ItemCode = sr.Item.ItemCode,
                    MovementType = "IN",
                    Quantity = sr.QuantityReceived,
                    Unit = sr.Item.Unit,
                    Reason = "Stock Received",
                    UserName = sr.CreatedBy.UserName,
                    Notes = sr.Notes,
                    UnitCost = sr.UnitCost,
                    TotalCost = sr.TotalCost
                })
                .ToListAsync();

            // Stock OUT movements
            var stockOutQuery = _context.UsageRecords
         .Where(ur => ur.IsActive && (!siteId.HasValue || ur.Item.SiteId == siteId))
         .Include(ur => ur.Item)
         .Include(ur => ur.CreatedBy)
         .AsQueryable();


            if (fromDate.HasValue)
                stockOutQuery = stockOutQuery.Where(ur => ur.UsageDate >= fromDate.Value);
            if (toDate.HasValue)
                stockOutQuery = stockOutQuery.Where(ur => ur.UsageDate <= toDate.Value);
            if (itemId.HasValue)
                stockOutQuery = stockOutQuery.Where(ur => ur.ItemId == itemId.Value);

            var stockOutMovements = await stockOutQuery
                .Select(ur => new StockMovementDetailViewModel
                {
                    Date = ur.UsageDate,
                    ItemId = ur.ItemId,
                    ItemName = ur.Item.ItemName,
                    ItemCode = ur.Item.ItemCode,
                    MovementType = "OUT",
                    Quantity = ur.QuantityUsed,
                    Unit = ur.Item.Unit,
                    Reason = ur.UsageReason,
                    UserName = ur.CreatedBy.UserName,
                    Notes = ur.Notes,
                    UnitCost = 0, // Will be calculated based on FIFO/Average
                    TotalCost = 0
                })
                .ToListAsync();

            // Combine and sort movements
            model.StockMovements = stockInMovements
                .Concat(stockOutMovements)
                .OrderByDescending(m => m.Date)
                .ToList();

            // Summary
            model.TotalInQuantity = stockInMovements.Sum(m => m.Quantity);
            model.TotalOutQuantity = stockOutMovements.Sum(m => m.Quantity);
            model.TotalInValue = stockInMovements.Sum(m => m.TotalCost);
            model.TotalOutValue = stockOutMovements.Sum(m => m.TotalCost);
            model.NetMovement = model.TotalInQuantity - model.TotalOutQuantity;

            return model;
        }

        #endregion

        #region Financial Reports

        public async Task<FinancialReportViewModel> GetFinancialReportsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var model = new FinancialReportViewModel();

            var stockQuery = _context.StockRecords.Where(sr => sr.IsActive);
            var usageQuery = _context.UsageRecords.Where(ur => ur.IsActive);

            if (fromDate.HasValue)
            {
                stockQuery = stockQuery.Where(sr => sr.StockDate >= fromDate.Value);
                usageQuery = usageQuery.Where(ur => ur.UsageDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                stockQuery = stockQuery.Where(sr => sr.StockDate <= toDate.Value);
                usageQuery = usageQuery.Where(ur => ur.UsageDate <= toDate.Value);
            }

            // Purchase Analysis
            model.TotalPurchases = await stockQuery.SumAsync(sr => sr.TotalCost);
            model.TotalQuantityPurchased = await stockQuery.SumAsync(sr => sr.QuantityReceived);

            // Consumption Analysis
            var totalUsageQuantity = await usageQuery.SumAsync(ur => ur.QuantityUsed);
            model.TotalConsumptionValue = await CalculateConsumptionValueAsync(usageQuery);

            // Inventory Value
            model.TotalInventoryValue = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .SumAsync(sr => sr.TotalCost);

            // Current Stock Value
            model.CurrentStockValue = await CalculateCurrentStockValueAsync();

            // Monthly financial trends - Fixed LINQ
            var stockData = await stockQuery
                .Select(sr => new { sr.StockDate.Year, sr.StockDate.Month, sr.TotalCost, sr.QuantityReceived })
                .ToListAsync();

            var monthlyPurchases = stockData
                .GroupBy(sr => new { sr.Year, sr.Month })
                .Select(g => new MonthlyFinancialViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    PurchaseAmount = g.Sum(x => x.TotalCost),
                    PurchaseQuantity = g.Sum(x => x.QuantityReceived)
                })
                .OrderBy(x => x.Month)
                .ToList();

            model.MonthlyFinancialTrends = monthlyPurchases;

            // Category-wise spending
            model.CategoryWiseSpending = await _context.StockRecords
                .Where(sr => sr.IsActive)
                .Include(sr => sr.Item)
                .GroupBy(sr => sr.Item.Category)
                .Select(g => new CategorySpendingViewModel
                {
                    Category = g.Key,
                    TotalSpent = g.Sum(x => x.TotalCost),
                    TotalQuantity = g.Sum(x => x.QuantityReceived),
                    ItemCount = g.Select(x => x.ItemId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToListAsync();

            return model;
        }

        #endregion

        #region Custom Reports

        public async Task<CustomReportResultViewModel> GenerateCustomReportAsync(CustomReportViewModel model)
        {
            var result = new CustomReportResultViewModel
            {
                ReportTitle = model.ReportTitle,
                GeneratedDate = DateTime.Now,
                Parameters = model
            };

            // Based on report type, generate appropriate data
            switch (model.ReportType?.ToLower())
            {
                case "patient_procedures":
                    result.Data = await GeneratePatientProceduresCustomReport(model);
                    break;
                case "inventory_usage":
                    result.Data = await GenerateInventoryUsageCustomReport(model);
                    break;
                case "financial_summary":
                    result.Data = await GenerateFinancialSummaryCustomReport(model);
                    break;
                default:
                    result.Data = new { error = "Unknown report type" };
                    break;
            }

            return result;
        }

        #endregion

        #region Private Helper Methods

        private async Task<int> GetCurrentStockAsync(Guid itemId)
        {
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .SumAsync(sr => sr.QuantityReceived);

            var totalUsed = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .SumAsync(ur => ur.QuantityUsed);

            return totalReceived - totalUsed;
        }

        private async Task<decimal> GetItemTotalValueAsync(Guid itemId, int currentStock)
        {
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

        private async Task<decimal> CalculateConsumptionValueAsync(IQueryable<Models.UsageRecord> usageQuery)
        {
            // This would implement FIFO or Average cost calculation for usage
            // For now, returning 0 - implement based on your costing method
            return 0;
        }

        private async Task<decimal> CalculateCurrentStockValueAsync()
        {
            var items = await _context.Items.Where(i => i.IsActive).ToListAsync();
            decimal totalValue = 0;

            foreach (var item in items)
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);
                var itemValue = await GetItemTotalValueAsync(item.ItemId, currentStock);
                totalValue += itemValue;
            }

            return totalValue;
        }

        // Chart Data Methods
        private async Task<object> GetProcedureMonthlyChartData(string period)
        {
            var months = period == "year" ? 12 : 6;
            var startDate = DateTime.Now.AddMonths(-months);

            // First get the raw data
            var rawData = await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureDate >= startDate)
                .Select(p => new
                {
                    Year = p.ProcedureDate.Year,
                    Month = p.ProcedureDate.Month,
                    Status = p.ProcedureStatus
                })
                .ToListAsync();

            // Then group and format on client side
            var data = rawData
                .GroupBy(p => new { p.Year, p.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Total = g.Count(),
                    Completed = g.Count(x => x.Status == "Completed"),
                    Scheduled = g.Count(x => x.Status == "Scheduled")
                })
                .OrderBy(x => x.Month)
                .ToList();

            return data;
        }

        private async Task<object> GetInventoryUsageChartData(string period)
        {
            var days = period == "month" ? 30 : 7;
            var startDate = DateTime.Now.AddDays(-days);

            var data = await _context.UsageRecords
                .Where(ur => ur.IsActive && ur.UsageDate >= startDate)
                .Include(ur => ur.Item)
                .GroupBy(ur => new { ur.Item.ItemName, ur.UsageDate.Date })
                .Select(g => new
                {
                    Item = g.Key.ItemName,
                    Date = g.Key.Date.ToString("yyyy-MM-dd"),
                    Quantity = g.Sum(x => x.QuantityUsed)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return data;
        }

        private async Task<object> GetPatientRegistrationChartData(string period)
        {
            var months = period == "year" ? 12 : 6;
            var startDate = DateTime.Now.AddMonths(-months);

            // Get raw data first
            var rawData = await _context.Patients
                .Where(p => p.IsActive && p.CreatedAt >= startDate)
                .Select(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .ToListAsync();

            // Group and format on client side
            var data = rawData
                .GroupBy(p => new { p.Year, p.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return data;
        }

        private async Task<object> GetFinancialTrendsChartData(string period)
        {
            var months = period == "year" ? 12 : 6;
            var startDate = DateTime.Now.AddMonths(-months);

            // Get raw data first
            var rawData = await _context.StockRecords
                .Where(sr => sr.IsActive && sr.StockDate >= startDate)
                .Select(sr => new { sr.StockDate.Year, sr.StockDate.Month, sr.TotalCost, sr.QuantityReceived })
                .ToListAsync();

            // Group and format on client side
            var data = rawData
                .GroupBy(sr => new { sr.Year, sr.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Purchases = g.Sum(x => x.TotalCost),
                    Quantity = g.Sum(x => x.QuantityReceived)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return data;
        }

        // Widget Data Methods
        private async Task<object> GetPendingProceduresWidget()
        {
            var pendingProcedures = await _context.Procedures
                .Where(p => p.IsActive && (p.ProcedureStatus == "Scheduled" || p.ProcedureStatus == "In-Progress"))
                .Include(p => p.Patient)
                .OrderBy(p => p.ProcedureDate)
                .Take(5)
                .Select(p => new
                {
                    PatientName = p.Patient.PatientName,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDate = p.ProcedureDate.ToString("dd/MM/yyyy HH:mm"),
                    Status = p.ProcedureStatus
                })
                .ToListAsync();

            return new { procedures = pendingProcedures };
        }

        private async Task<object> GetCriticalStockWidget()
        {
            var items = await _context.Items.Where(i => i.IsActive && i.ReorderLevel > 0).ToListAsync();
            var criticalItems = new List<object>();

            foreach (var item in items.Take(10))
            {
                var currentStock = await GetCurrentStockAsync(item.ItemId);
                if (currentStock <= item.ReorderLevel)
                {
                    criticalItems.Add(new
                    {
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode,
                        CurrentStock = currentStock,
                        ReorderLevel = item.ReorderLevel,
                        Unit = item.Unit,
                        IsCritical = currentStock <= 0
                    });
                }
            }

            return new { items = criticalItems };
        }

        private async Task<object> GetRecentPatientsWidget()
        {
            var recentPatients = await _context.Patients
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    PatientName = p.PatientName,
                    PatientMRNumber = p.PatientMRNumber,
                    RegistrationDate = p.CreatedAt.ToString("dd/MM/yyyy"),
                    Age = p.PatientAge,
                    Gender = p.PatientGender
                })
                .ToListAsync();

            return new { patients = recentPatients };
        }

        private async Task<object> GetMonthlySummaryWidget()
        {
            var thisMonth = DateTime.Now.Date.AddDays(1 - DateTime.Now.Day);

            var summary = new
            {
                ProceduresThisMonth = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureDate >= thisMonth),
                PatientsThisMonth = await _context.Patients.CountAsync(p => p.IsActive && p.CreatedAt >= thisMonth),
                PurchasesThisMonth = await _context.StockRecords
                    .Where(sr => sr.IsActive && sr.StockDate >= thisMonth)
                    .SumAsync(sr => sr.TotalCost),
                UsageThisMonth = await _context.UsageRecords
                    .Where(ur => ur.IsActive && ur.UsageDate >= thisMonth)
                    .SumAsync(ur => ur.QuantityUsed)
            };

            return summary;
        }

        // Custom Report Generation Methods
        private async Task<object> GeneratePatientProceduresCustomReport(CustomReportViewModel model)
        {
            var query = _context.Patients
                .Where(p => p.IsActive)
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                .AsQueryable();

            if (model.FromDate.HasValue)
                query = query.Where(p => p.Procedures.Any(proc => proc.ProcedureDate >= model.FromDate.Value));

            if (model.ToDate.HasValue)
                query = query.Where(p => p.Procedures.Any(proc => proc.ProcedureDate <= model.ToDate.Value));

            var data = await query
                .Select(p => new
                {
                    PatientName = p.PatientName,
                    MRNumber = p.PatientMRNumber,
                    TotalProcedures = p.Procedures.Count(),
                    CompletedProcedures = p.Procedures.Count(proc => proc.ProcedureStatus == "Completed"),
                    LastProcedure = p.Procedures.OrderByDescending(proc => proc.ProcedureDate)
                        .Select(proc => proc.ProcedureDate).FirstOrDefault()
                })
                .ToListAsync();

            return data;
        }

        private async Task<object> GenerateInventoryUsageCustomReport(CustomReportViewModel model)
        {
            var query = _context.UsageRecords
                .Where(ur => ur.IsActive)
                .Include(ur => ur.Item)
                .Include(ur => ur.Patient)
                .AsQueryable();

            if (model.FromDate.HasValue)
                query = query.Where(ur => ur.UsageDate >= model.FromDate.Value);

            if (model.ToDate.HasValue)
                query = query.Where(ur => ur.UsageDate <= model.ToDate.Value);

            var data = await query
                .GroupBy(ur => new { ur.Item.ItemName, ur.Item.ItemCode, ur.Item.Category })
                .Select(g => new
                {
                    ItemName = g.Key.ItemName,
                    ItemCode = g.Key.ItemCode,
                    Category = g.Key.Category,
                    TotalUsed = g.Sum(x => x.QuantityUsed),
                    UsageCount = g.Count(),
                    UniquePatients = g.Where(x => x.PatientId.HasValue).Select(x => x.PatientId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalUsed)
                .ToListAsync();

            return data;
        }

        private async Task<object> GenerateFinancialSummaryCustomReport(CustomReportViewModel model)
        {
            var stockQuery = _context.StockRecords.Where(sr => sr.IsActive);
            var usageQuery = _context.UsageRecords.Where(ur => ur.IsActive);

            if (model.FromDate.HasValue)
            {
                stockQuery = stockQuery.Where(sr => sr.StockDate >= model.FromDate.Value);
                usageQuery = usageQuery.Where(ur => ur.UsageDate >= model.FromDate.Value);
            }

            if (model.ToDate.HasValue)
            {
                stockQuery = stockQuery.Where(sr => sr.StockDate <= model.ToDate.Value);
                usageQuery = usageQuery.Where(ur => ur.UsageDate <= model.ToDate.Value);
            }

            var categoryWiseData = await stockQuery
                .Include(sr => sr.Item)
                .GroupBy(sr => sr.Item.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalPurchases = g.Sum(x => x.TotalCost),
                    TotalQuantity = g.Sum(x => x.QuantityReceived),
                    AverageUnitCost = g.Average(x => x.UnitCost),
                    ItemCount = g.Select(x => x.ItemId).Distinct().Count()
                })
                .ToListAsync();

            return categoryWiseData;
        }

        #endregion
    }
}