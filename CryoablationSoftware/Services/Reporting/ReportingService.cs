using ClosedXML.Excel;
using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;
using CryoablationSoftware.ViewModel.Reporting;
using Microsoft.EntityFrameworkCore;
using SelectPdf;

namespace CryoablationSoftware.Services.Reporting
{
    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentSiteService _currentSiteService;

        public ReportingService(ApplicationDbContext context, IWebHostEnvironment env, ICurrentSiteService currentSiteService)
        {
            _context = context;
            _env = env;
            _currentSiteService = currentSiteService;
        }

        #region Main Reporting Methods

        public async Task<ReportingIndexViewModel> GetReportingIndexAsync()
        {
            var model = new ReportingIndexViewModel
            {
                TotalReports = 16, // Static count of available report types
                ReportsThisMonth = await GetReportsGeneratedThisMonthAsync()
            };

            // Define report categories
            model.ReportCategories = new List<ReportCategoryViewModel>
            {
                new()
                {
                    CategoryName = "Patient Reports",
                    CategoryIcon = "fas fa-user-injured",
                    ReportCount = 2,
                    Templates = new List<ReportTemplateViewModel>
                    {
                        new() { TemplateName = "Patient Procedure Report", Description = "Complete patient procedure history", ReportType = "PatientProcedure", RequiresDateRange = true, RequiresPatientSelection = true },
                        new() { TemplateName = "Patient Summary Report", Description = "Patient overview with key metrics", ReportType = "PatientSummary", RequiresPatientSelection = true }
                    }
                },
                new()
                {
                    CategoryName = "Inventory Reports",
                    CategoryIcon = "fas fa-boxes",
                    ReportCount = 4,
                    Templates = new List<ReportTemplateViewModel>
                    {
                        new() { TemplateName = "Inventory Consumption Report", Description = "Detailed inventory usage analysis", ReportType = "InventoryConsumption", RequiresDateRange = true, RequiresItemSelection = true },
                        new() { TemplateName = "Stock Movement Report", Description = "Complete stock in/out movements", ReportType = "StockMovement", RequiresDateRange = true, RequiresItemSelection = true },
                        new() { TemplateName = "Low Stock Alert Report", Description = "Items below reorder level", ReportType = "LowStockAlert" },
                        new() { TemplateName = "Item Valuation Report", Description = "Current inventory valuation", ReportType = "ItemValuation", RequiresItemSelection = true }
                    }
                },
                new()
                {
                    CategoryName = "Financial Reports",
                    CategoryIcon = "fas fa-chart-line",
                    ReportCount = 3,
                    Templates = new List<ReportTemplateViewModel>
                    {
                        new() { TemplateName = "Financial Summary Report", Description = "Comprehensive financial analysis", ReportType = "FinancialSummary", RequiresDateRange = true },
                        new() { TemplateName = "Cost Analysis Report", Description = "Detailed cost breakdown", ReportType = "CostAnalysis", RequiresDateRange = true },
                        new() { TemplateName = "Profitability Analysis", Description = "Revenue vs cost analysis", ReportType = "ProfitabilityAnalysis", RequiresDateRange = true }
                    }
                }
            };

            // Get recent reports (mock data for now)
            model.RecentReports = await GetRecentReportsAsync();

            return model;
        }

        public async Task<List<ReportTemplateViewModel>> GetReportTemplatesAsync()
        {
            return new List<ReportTemplateViewModel>
            {
                new() { TemplateName = "Patient Procedure Report", Description = "Complete patient procedure history", ReportType = "PatientProcedure", RequiresDateRange = true, RequiresPatientSelection = true },
                new() { TemplateName = "Inventory Consumption Report", Description = "Detailed inventory usage analysis", ReportType = "InventoryConsumption", RequiresDateRange = true, RequiresItemSelection = true },
                new() { TemplateName = "Financial Summary Report", Description = "Comprehensive financial analysis", ReportType = "FinancialSummary", RequiresDateRange = true },
                new() { TemplateName = "Stock Movement Report", Description = "Complete stock movements", ReportType = "StockMovement", RequiresDateRange = true },
                new() { TemplateName = "Low Stock Alert Report", Description = "Critical stock levels", ReportType = "LowStockAlert" }
            };
        }

        public async Task<GeneratedReportViewModel> GenerateReportAsync(ReportGenerationViewModel model, string userId)
        {
            var report = new GeneratedReportViewModel
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportTitle = model.ReportTitle,
                ReportType = model.ReportType,
                GeneratedDate = DateTime.Now,
                GeneratedBy = userId,
                Parameters = model
            };

            var startTime = DateTime.Now;

            try
            {
                // Generate report data based on type
                report.ReportData = model.ReportType switch
                {
                    "PatientProcedure" => await GeneratePatientProcedureDataAsync(model),
                    "InventoryConsumption" => await GenerateInventoryConsumptionDataAsync(model),
                    "FinancialSummary" => await GenerateFinancialSummaryDataAsync(model),
                    "StockMovement" => await GenerateStockMovementDataAsync(model),
                    "LowStockAlert" => await GenerateLowStockAlertDataAsync(model),
                    "ProcedureAnalysis" => await GenerateProcedureAnalysisDataAsync(model),
                    _ => throw new ArgumentException($"Unknown report type: {model.ReportType}")
                };

                // Generate summary
                report.Summary = GenerateReportSummary(report.ReportData, model.ReportType);

                // Generate sections
                report.Sections = GenerateReportSections(report.ReportData, model);

                report.GenerationTime = DateTime.Now - startTime;
                report.TotalRecords = GetTotalRecords(report.ReportData);

                return report;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating report: {ex.Message}", ex);
            }
        }

        #endregion

        #region Specific Report Generators

        public async Task<PatientProcedureReportViewModel> GeneratePatientProcedureReportAsync(Guid patientId, DateTime? fromDate, DateTime? toDate)
        {
            var patient = await _context.Patients
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                    .ThenInclude(proc => proc.ItemUsages.Where(iu => iu.IsActive))
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                    .ThenInclude(proc => proc.CreatedBy)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive);

            if (patient == null) return null;

            var procedures = patient.Procedures.AsQueryable();
            if (fromDate.HasValue)
                procedures = procedures.Where(p => p.ProcedureDate >= fromDate.Value);
            if (toDate.HasValue)
                procedures = procedures.Where(p => p.ProcedureDate <= toDate.Value);

            var procedureList = procedures.OrderByDescending(p => p.ProcedureDate).ToList();

            var report = new PatientProcedureReportViewModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.PatientName,
                PatientMRNumber = patient.PatientMRNumber,
                PatientPhone = patient.PatientPhone,
                PatientAge = patient.PatientAge,
                PatientGender = patient.PatientGender,
                RegistrationDate = patient.CreatedAt,

                Procedures = procedureList.Select(p => new ProcedureReportDetailViewModel
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    ProcedureRemarks = p.ProcedureRemarks,
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy?.UserName ?? "Unknown"
                }).ToList(),

                Summary = new PatientReportSummaryViewModel
                {
                    TotalProcedures = procedureList.Count,
                    CompletedProcedures = procedureList.Count(p => p.ProcedureStatus == "Completed"),
                    ScheduledProcedures = procedureList.Count(p => p.ProcedureStatus == "Scheduled"),
                    CancelledProcedures = procedureList.Count(p => p.ProcedureStatus == "Cancelled"),
                    TotalItemsUsed = procedureList.Sum(p => p.ItemUsages.Sum(iu => iu.QuantityUsed)),
                    LastProcedureDate = procedureList.FirstOrDefault()?.ProcedureDate ?? DateTime.MinValue,
                    MostCommonROI = procedureList.GroupBy(p => p.ProcedureROI).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "N/A"
                }
            };

            return report;
        }

        public async Task<InventoryConsumptionReportViewModel> GenerateInventoryConsumptionReportAsync(DateTime fromDate, DateTime toDate, List<string> categories = null)
        {
            var itemsQuery = _context.Items.Where(i => i.IsActive);

            if (categories != null && categories.Any())
                itemsQuery = itemsQuery.Where(i => categories.Contains(i.Category));

            var items = await itemsQuery.ToListAsync();
            var itemDetails = new List<ItemConsumptionDetailViewModel>();

            foreach (var item in items)
            {
                var openingStock = await GetOpeningStockAsync(item.ItemId, fromDate);
                var stockReceived = await GetStockReceivedAsync(item.ItemId, fromDate, toDate);
                var stockUsed = await GetStockUsedAsync(item.ItemId, fromDate, toDate);
                var closingStock = openingStock + stockReceived - stockUsed;
                var procedureCount = await GetProcedureCountForItemAsync(item.ItemId, fromDate, toDate);

                itemDetails.Add(new ItemConsumptionDetailViewModel
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Category = item.Category,
                    Unit = item.Unit,
                    OpeningStock = openingStock,
                    StockReceived = stockReceived,
                    StockUsed = stockUsed,
                    ClosingStock = closingStock,
                    ProcedureCount = procedureCount,
                    ConsumptionRate = procedureCount > 0 ? (double)stockUsed / procedureCount : 0
                });
            }

            var categoryBreakdown = itemDetails
                .GroupBy(i => i.Category)
                .Select(g => new CategoryConsumptionViewModel
                {
                    Category = g.Key,
                    ItemCount = g.Count(),
                    TotalConsumed = g.Sum(x => x.StockUsed),
                    TotalValue = g.Sum(x => x.TotalValue),
                    PercentageOfTotal = itemDetails.Sum(x => x.StockUsed) > 0
                        ? (double)g.Sum(x => x.StockUsed) / itemDetails.Sum(x => x.StockUsed) * 100
                        : 0
                })
                .OrderByDescending(c => c.TotalConsumed)
                .ToList();

            return new InventoryConsumptionReportViewModel
            {
                Items = itemDetails.OrderByDescending(i => i.StockUsed).ToList(),
                CategoryBreakdown = categoryBreakdown,
                Summary = new InventoryConsumptionSummaryViewModel
                {
                    TotalItemsConsumed = itemDetails.Sum(i => i.StockUsed),
                    TotalProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureDate >= fromDate && p.ProcedureDate <= toDate),
                    HighestConsumptionCategory = categoryBreakdown.FirstOrDefault()?.Category ?? "N/A",
                    HighestConsumptionItem = itemDetails.OrderByDescending(i => i.StockUsed).FirstOrDefault()?.ItemName ?? "N/A"
                }
            };
        }

        public async Task<FinancialSummaryReportViewModel> GenerateFinancialSummaryReportAsync(DateTime fromDate, DateTime toDate)
        {
            var purchases = await _context.StockRecords
                .Where(sr => sr.IsActive && sr.StockDate >= fromDate && sr.StockDate <= toDate)
                .ToListAsync();

            var usage = await _context.UsageRecords
                .Where(ur => ur.IsActive && ur.UsageDate >= fromDate && ur.UsageDate <= toDate)
                .ToListAsync();

            var procedures = await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureDate >= fromDate && p.ProcedureDate <= toDate)
                .ToListAsync();

            // Monthly data
            var monthlyData = purchases
                .GroupBy(p => new { p.StockDate.Year, p.StockDate.Month })
                .Select(g => new MonthlyFinancialDataViewModel
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Purchases = g.Sum(x => x.TotalCost),
                    ProcedureCount = procedures.Count(p => p.ProcedureDate.Year == g.Key.Year && p.ProcedureDate.Month == g.Key.Month)
                })
                .OrderBy(m => m.Month)
                .ToList();

            // Category data
            var categoryData = purchases
                .GroupBy(p => p.Item.Category)
                .Select(g => new CategoryFinancialDataViewModel
                {
                    Category = g.Key,
                    PurchaseAmount = g.Sum(x => x.TotalCost),
                    ItemCount = g.Select(x => x.ItemId).Distinct().Count()
                })
                .ToList();

            return new FinancialSummaryReportViewModel
            {
                ReportPeriodFrom = fromDate,
                ReportPeriodTo = toDate,
                Metrics = new FinancialMetricsViewModel
                {
                    InventoryPurchases = purchases.Sum(p => p.TotalCost),
                    TotalProcedures = procedures.Count,
                    AverageCostPerProcedure = procedures.Count > 0 ? purchases.Sum(p => p.TotalCost) / procedures.Count : 0
                },
                MonthlyData = monthlyData,
                CategoryData = categoryData,
                Analysis = new FinancialAnalysisViewModel
                {
                    KeyInsights = GenerateFinancialInsights(monthlyData, categoryData),
                    Recommendations = GenerateFinancialRecommendations(monthlyData, categoryData)
                }
            };
        }

        #endregion

        #region Export Methods

        public async Task<byte[]> ExportReportToPdfAsync(GeneratedReportViewModel report)
        {
            try
            {
                var htmlContent = GenerateReportHtml(report);
                return ConvertHtmlToPdf(htmlContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting report to PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(GeneratedReportViewModel report)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(report.ReportTitle);

                // Add header
                worksheet.Cell(1, 1).Value = report.ReportTitle;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;

                // Add generation info
                worksheet.Cell(2, 1).Value = $"Generated on: {report.GeneratedDate:dd/MM/yyyy HH:mm}";
                worksheet.Cell(3, 1).Value = $"Report Type: {report.ReportType}";

                // Add data based on report type
                int row = 5;
                row = AddReportDataToExcel(worksheet, report, row);

                // Auto-fit columns
                worksheet.ColumnsUsed().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting report to Excel: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ExportReportToWordAsync(GeneratedReportViewModel report)
        {
            // For now, return PDF as Word export would require additional libraries
            return await ExportReportToPdfAsync(report);
        }

        #endregion

        #region Utility Methods

        public async Task<List<PatientSummaryViewModel>> GetPatientsForReportAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            var query = _context.Patients.Where(p => p.IsActive);
            if (siteId.HasValue)
                query = query.Where(p => p.SiteId == siteId);

            return await query
                .Select(p => new PatientSummaryViewModel
                {
                    PatientId = p.PatientId,
                    PatientName = p.PatientName,
                    PatientMRNumber = p.PatientMRNumber,
                    PatientPhone = p.PatientPhone,
                    PatientAge = p.PatientAge,
                    PatientGender = p.PatientGender,
                    RegistrationDate = p.CreatedAt
                })
                .OrderBy(p => p.PatientName)
                .ToListAsync();
        }

        public async Task<List<ItemSummaryViewModel>> GetItemsForReportAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            var query = _context.Items.Where(i => i.IsActive);
            if (siteId.HasValue)
                query = query.Where(i => i.SiteId == siteId);

            return await query
                .Select(i => new ItemSummaryViewModel
                {
                    ItemId = i.ItemId,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Category = i.Category
                })
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }


        public async Task<List<string>> GetCategoriesForReportAsync()
        {
            var siteId = await _currentSiteService.GetCurrentSiteIdAsync();
            var isSuperAdmin = await _currentSiteService.IsSuperAdminAsync();

            var query = _context.Items.Where(i => i.IsActive);
            if (siteId.HasValue)
                query = query.Where(i => i.SiteId == siteId);

            return await query
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }


        public async Task<ReportValidationResult> ValidateReportParametersAsync(ReportGenerationViewModel model)
        {
            var result = new ReportValidationResult { IsValid = true };

            // Validate date range
            if (model.FromDate.HasValue && model.ToDate.HasValue && model.FromDate > model.ToDate)
            {
                result.IsValid = false;
                result.Errors.Add("From date cannot be greater than To date");
            }

            // Validate patient selection
            if (model.ReportType == "PatientProcedure" && !model.IncludeAllPatients && !model.SelectedPatients.Any())
            {
                result.IsValid = false;
                result.Errors.Add("Please select at least one patient for Patient Procedure report");
            }

            return result;
        }

        #endregion

        #region Private Helper Methods

        private async Task<int> GetReportsGeneratedThisMonthAsync()
        {
            // This would be implemented with a reports log table
            // For now returning a mock value
            return 15;
        }

        private async Task<List<RecentReportViewModel>> GetRecentReportsAsync()
        {
            // This would come from a reports log table
            // For now returning mock data
            return new List<RecentReportViewModel>
            {
                new() { ReportName = "Monthly Inventory Report", ReportType = "Inventory", GeneratedDate = DateTime.Now.AddDays(-2), GeneratedBy = "Admin", Status = "Completed" },
                new() { ReportName = "Patient Analysis Report", ReportType = "Patient", GeneratedDate = DateTime.Now.AddDays(-5), GeneratedBy = "Doctor", Status = "Completed" }
            };
        }

        private async Task<object> GeneratePatientProcedureDataAsync(ReportGenerationViewModel model)
        {
            var query = _context.Patients
                .Include(p => p.Procedures.Where(proc => proc.IsActive))
                .Where(p => p.IsActive);

            if (!model.IncludeAllPatients && model.SelectedPatients.Any())
                query = query.Where(p => model.SelectedPatients.Contains(p.PatientId));

            if (model.FromDate.HasValue)
                query = query.Where(p => p.Procedures.Any(proc => proc.ProcedureDate >= model.FromDate.Value));

            if (model.ToDate.HasValue)
                query = query.Where(p => p.Procedures.Any(proc => proc.ProcedureDate <= model.ToDate.Value));

            return await query.Take(100).ToListAsync(); // Limit for performance
        }

        private async Task<object> GenerateInventoryConsumptionDataAsync(ReportGenerationViewModel model)
        {
            var fromDate = model.FromDate ?? DateTime.Now.AddMonths(-1);
            var toDate = model.ToDate ?? DateTime.Now;
            var categories = model.SelectedCategories?.Any() == true ? model.SelectedCategories : null;

            return await GenerateInventoryConsumptionReportAsync(fromDate, toDate, categories);
        }

        private async Task<object> GenerateFinancialSummaryDataAsync(ReportGenerationViewModel model)
        {
            var fromDate = model.FromDate ?? DateTime.Now.AddMonths(-3);
            var toDate = model.ToDate ?? DateTime.Now;

            return await GenerateFinancialSummaryReportAsync(fromDate, toDate);
        }

        private async Task<object> GenerateStockMovementDataAsync(ReportGenerationViewModel model)
        {
            var query = _context.StockRecords
                .Include(sr => sr.Item)
                .Include(sr => sr.CreatedBy)
                .Where(sr => sr.IsActive);

            if (model.FromDate.HasValue)
                query = query.Where(sr => sr.StockDate >= model.FromDate.Value);

            if (model.ToDate.HasValue)
                query = query.Where(sr => sr.StockDate <= model.ToDate.Value);

            return await query.OrderByDescending(sr => sr.StockDate).Take(1000).ToListAsync();
        }

        private async Task<object> GenerateLowStockAlertDataAsync(ReportGenerationViewModel model)
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

            return lowStockItems.OrderBy(i => i.CurrentStock).ToList();
        }

        private async Task<object> GenerateProcedureAnalysisDataAsync(ReportGenerationViewModel model)
        {
            var query = _context.Procedures
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .Where(p => p.IsActive);

            if (model.FromDate.HasValue)
                query = query.Where(p => p.ProcedureDate >= model.FromDate.Value);

            if (model.ToDate.HasValue)
                query = query.Where(p => p.ProcedureDate <= model.ToDate.Value);

            if (!string.IsNullOrEmpty(model.StatusFilter))
                query = query.Where(p => p.ProcedureStatus == model.StatusFilter);

            return await query.OrderByDescending(p => p.ProcedureDate).Take(500).ToListAsync();
        }

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

        private async Task<int> GetOpeningStockAsync(Guid itemId, DateTime fromDate)
        {
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive && sr.StockDate < fromDate)
                .SumAsync(sr => sr.QuantityReceived);

            var totalUsed = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive && ur.UsageDate < fromDate)
                .SumAsync(ur => ur.QuantityUsed);

            return totalReceived - totalUsed;
        }

        private async Task<int> GetStockReceivedAsync(Guid itemId, DateTime fromDate, DateTime toDate)
        {
            return await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive && sr.StockDate >= fromDate && sr.StockDate <= toDate)
                .SumAsync(sr => sr.QuantityReceived);
        }

        private async Task<int> GetStockUsedAsync(Guid itemId, DateTime fromDate, DateTime toDate)
        {
            return await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive && ur.UsageDate >= fromDate && ur.UsageDate <= toDate)
                .SumAsync(ur => ur.QuantityUsed);
        }

        private async Task<int> GetProcedureCountForItemAsync(Guid itemId, DateTime fromDate, DateTime toDate)
        {
            return await _context.ProcedureItemUsages
                .Where(piu => piu.ItemId == itemId && piu.IsActive &&
                             piu.UsageDateTime >= fromDate && piu.UsageDateTime <= toDate)
                .Select(piu => piu.ProcedureId)
                .Distinct()
                .CountAsync();
        }

        private ReportSummaryViewModel GenerateReportSummary(object reportData, string reportType)
        {
            var summary = new ReportSummaryViewModel();

            switch (reportType)
            {
                case "PatientProcedure":
                    if (reportData is List<Patient> patients)
                    {
                        summary.KeyMetrics["Total Patients"] = patients.Count;
                        summary.KeyMetrics["Total Procedures"] = patients.Sum(p => p.Procedures.Count);
                    }
                    break;
                case "InventoryConsumption":
                    if (reportData is InventoryConsumptionReportViewModel inventory)
                    {
                        summary.KeyMetrics["Items Analyzed"] = inventory.Items.Count;
                        summary.KeyMetrics["Total Consumption"] = inventory.Summary.TotalItemsConsumed;
                    }
                    break;
            }

            return summary;
        }

        private List<ReportSectionViewModel> GenerateReportSections(object reportData, ReportGenerationViewModel model)
        {
            var sections = new List<ReportSectionViewModel>();

            if (model.IncludeSummary)
            {
                sections.Add(new ReportSectionViewModel
                {
                    SectionTitle = "Executive Summary",
                    SectionType = "Summary",
                    SectionData = GenerateExecutiveSummary(reportData, model.ReportType)
                });
            }

            if (model.IncludeDetailedData)
            {
                sections.Add(new ReportSectionViewModel
                {
                    SectionTitle = "Detailed Data",
                    SectionType = "Table",
                    SectionData = reportData
                });
            }

            return sections;
        }

        private object GenerateExecutiveSummary(object reportData, string reportType)
        {
            return new { Summary = $"Executive summary for {reportType} report generated on {DateTime.Now:dd/MM/yyyy}" };
        }

        private int GetTotalRecords(object reportData)
        {
            return reportData switch
            {
                System.Collections.ICollection collection => collection.Count,
                IEnumerable<object> enumerable => enumerable.Count(),
                _ => 1
            };
        }

        private List<string> GenerateFinancialInsights(List<MonthlyFinancialDataViewModel> monthlyData, List<CategoryFinancialDataViewModel> categoryData)
        {
            var insights = new List<string>();

            if (monthlyData.Count > 1)
            {
                var growth = ((monthlyData.Last().Purchases - monthlyData.First().Purchases) / monthlyData.First().Purchases) * 100;
                insights.Add($"Purchase growth rate: {growth:F1}% over the period");
            }

            if (categoryData.Any())
            {
                var topCategory = categoryData.OrderByDescending(c => c.PurchaseAmount).First();
                insights.Add($"Highest spending category: {topCategory.Category} (Rs {topCategory.PurchaseAmount:N0})");
            }

            return insights;
        }

        private List<string> GenerateFinancialRecommendations(List<MonthlyFinancialDataViewModel> monthlyData, List<CategoryFinancialDataViewModel> categoryData)
        {
            var recommendations = new List<string>
            {
                "Monitor monthly spending trends for budget planning",
                "Review high-cost categories for optimization opportunities",
                "Consider bulk purchasing for frequently used items"
            };

            return recommendations;
        }

        private string GenerateReportHtml(GeneratedReportViewModel report)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{report.ReportTitle}</title>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 20px; 
            color: #333; 
            line-height: 1.6;
        }}
        .header {{ 
            text-align: center; 
            border-bottom: 2px solid #667eea; 
            padding-bottom: 20px; 
            margin-bottom: 30px;
        }}
        .report-title {{ 
            font-size: 24px; 
            font-weight: bold; 
            color: #667eea; 
            margin-bottom: 10px;
        }}
        .report-info {{ 
            color: #666; 
            font-size: 12px;
        }}
        .section {{ 
            margin: 25px 0; 
            page-break-inside: avoid;
        }}
        .section-title {{ 
            font-size: 18px; 
            font-weight: bold; 
            color: #667eea; 
            margin-bottom: 15px;
            padding-bottom: 8px;
            border-bottom: 1px solid #dee2e6;
        }}
        table {{ 
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
            font-size: 12px;
        }}
        th, td {{ 
            border: 1px solid #ddd; 
            padding: 8px; 
            text-align: left;
        }}
        th {{ 
            background-color: #f8f9fa; 
            font-weight: bold;
        }}
        .summary-box {{
            background: #f8f9fa;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin: 15px 0;
        }}
        .footer {{ 
            text-align: center; 
            margin-top: 40px; 
            padding-top: 20px; 
            border-top: 1px solid #dee2e6; 
            font-size: 10px; 
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='report-title'>{report.ReportTitle}</div>
        <div class='report-info'>
            Generated on: {report.GeneratedDate:dd/MM/yyyy HH:mm} | 
            Report Type: {report.ReportType} | 
            Total Records: {report.TotalRecords}
        </div>
    </div>
    
    {GenerateHtmlContent(report)}
    
    <div class='footer'>
        <div>CryoablationSoftware - Hospital Management System</div>
        <div>Report generated in {report.GenerationTime.TotalSeconds:F1} seconds</div>
    </div>
</body>
</html>";
        }

        private string GenerateHtmlContent(GeneratedReportViewModel report)
        {
            var content = "";

            // Add summary section
            if (report.Summary?.KeyMetrics?.Any() == true)
            {
                content += "<div class='section'>";
                content += "<div class='section-title'>Summary</div>";
                content += "<div class='summary-box'>";
                foreach (var metric in report.Summary.KeyMetrics)
                {
                    content += $"<div><strong>{metric.Key}:</strong> {metric.Value}</div>";
                }
                content += "</div></div>";
            }

            // Add sections
            if (report.Sections?.Any() == true)
            {
                foreach (var section in report.Sections)
                {
                    content += $"<div class='section'>";
                    content += $"<div class='section-title'>{section.SectionTitle}</div>";
                    content += GenerateSectionHtml(section);
                    content += "</div>";
                }
            }

            return content;
        }

        private string GenerateSectionHtml(ReportSectionViewModel section)
        {
            return section.SectionType switch
            {
                "Table" => GenerateTableHtml(section.SectionData),
                "Summary" => GenerateSummaryHtml(section.SectionData),
                _ => "<div>Section content not available</div>"
            };
        }

        private string GenerateTableHtml(object data)
        {
            // This is a simplified implementation
            // In a real scenario, you'd generate proper table HTML based on the data type
            return "<div>Table data would be rendered here</div>";
        }

        private string GenerateSummaryHtml(object data)
        {
            return "<div class='summary-box'>Summary information would be displayed here</div>";
        }

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            try
            {
                var converter = new HtmlToPdf();

                // Configure PDF settings
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.WebPageWidth = 1024;
                converter.Options.WebPageHeight = 0;

                // Set margins
                converter.Options.MarginTop = 20;
                converter.Options.MarginBottom = 20;
                converter.Options.MarginLeft = 20;
                converter.Options.MarginRight = 20;

                converter.Options.JavaScriptEnabled = false;
                converter.Options.EmbedFonts = true;
                converter.Options.CssMediaType = HtmlToPdfCssMediaType.Print;

                var pdfDocument = converter.ConvertHtmlString(htmlContent);
                var pdfBytes = pdfDocument.Save();
                pdfDocument.Close();

                return pdfBytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting HTML to PDF: {ex.Message}", ex);
            }
        }

        private int AddReportDataToExcel(IXLWorksheet worksheet, GeneratedReportViewModel report, int startRow)
        {
            int currentRow = startRow;

            // Add summary data
            if (report.Summary?.KeyMetrics?.Any() == true)
            {
                worksheet.Cell(currentRow, 1).Value = "SUMMARY";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow += 2;

                foreach (var metric in report.Summary.KeyMetrics)
                {
                    worksheet.Cell(currentRow, 1).Value = metric.Key;
                    worksheet.Cell(currentRow, 2).Value = metric.Value.ToString();
                    currentRow++;
                }
                currentRow += 2;
            }

            // Add detailed data based on report type
            switch (report.ReportType)
            {
                case "PatientProcedure":
                    currentRow = AddPatientProcedureDataToExcel(worksheet, report.ReportData, currentRow);
                    break;
                case "InventoryConsumption":
                    currentRow = AddInventoryDataToExcel(worksheet, report.ReportData, currentRow);
                    break;
                    // Add other report types as needed
            }

            return currentRow;
        }

        private int AddPatientProcedureDataToExcel(IXLWorksheet worksheet, object data, int startRow)
        {
            worksheet.Cell(startRow, 1).Value = "PATIENT PROCEDURE DATA";
            worksheet.Cell(startRow, 1).Style.Font.Bold = true;
            startRow += 2;

            // Add headers
            worksheet.Cell(startRow, 1).Value = "Patient Name";
            worksheet.Cell(startRow, 2).Value = "MR Number";
            worksheet.Cell(startRow, 3).Value = "Total Procedures";
            worksheet.Cell(startRow, 4).Value = "Last Procedure";

            var headerRange = worksheet.Range(startRow, 1, startRow, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            startRow++;

            // Add data rows (simplified - you'd iterate through actual data)
            if (data is List<Patient> patients)
            {
                foreach (var patient in patients.Take(100)) // Limit for performance
                {
                    worksheet.Cell(startRow, 1).Value = patient.PatientName;
                    worksheet.Cell(startRow, 2).Value = patient.PatientMRNumber;
                    worksheet.Cell(startRow, 3).Value = patient.Procedures?.Count ?? 0;
                    worksheet.Cell(startRow, 4).Value = patient.Procedures?.OrderByDescending(p => p.ProcedureDate).FirstOrDefault()?.ProcedureDate.ToString("dd/MM/yyyy") ?? "N/A";
                    startRow++;
                }
            }

            return startRow + 2;
        }

        private int AddInventoryDataToExcel(IXLWorksheet worksheet, object data, int startRow)
        {
            worksheet.Cell(startRow, 1).Value = "INVENTORY CONSUMPTION DATA";
            worksheet.Cell(startRow, 1).Style.Font.Bold = true;
            startRow += 2;

            // Add headers
            worksheet.Cell(startRow, 1).Value = "Item Code";
            worksheet.Cell(startRow, 2).Value = "Item Name";
            worksheet.Cell(startRow, 3).Value = "Category";
            worksheet.Cell(startRow, 4).Value = "Stock Used";
            worksheet.Cell(startRow, 5).Value = "Unit";

            var headerRange = worksheet.Range(startRow, 1, startRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

            startRow++;

            if (data is InventoryConsumptionReportViewModel inventory)
            {
                foreach (var item in inventory.Items.Take(100))
                {
                    worksheet.Cell(startRow, 1).Value = item.ItemCode;
                    worksheet.Cell(startRow, 2).Value = item.ItemName;
                    worksheet.Cell(startRow, 3).Value = item.Category;
                    worksheet.Cell(startRow, 4).Value = item.StockUsed;
                    worksheet.Cell(startRow, 5).Value = item.Unit;
                    startRow++;
                }
            }

            return startRow + 2;
        }

        #endregion
    }
}