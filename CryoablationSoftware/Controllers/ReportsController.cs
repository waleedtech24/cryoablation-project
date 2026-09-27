using ClosedXML.Excel;
using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelectPdf;

namespace CryoablationSoftware.Controllers
{
    [Authorize]
    public class ReportsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportsService _reportsService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICurrentSiteService _currentSiteService;

        public ReportsController(
            ApplicationDbContext context,
            IReportsService reportsService,
            UserManager<IdentityUser> userManager, ICurrentSiteService currentSiteService)
        : base(currentSiteService, context)
        {
            _context = context;
            _reportsService = reportsService;
            _userManager = userManager;
            _currentSiteService = currentSiteService;
        }

        #region Dashboard & Analytics

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _reportsService.GetReportsDashboardAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            var model = await _reportsService.GetAnalyticsDataAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetChartData(string chartType, string period = "month")
        {
            var data = await _reportsService.GetChartDataAsync(chartType, period);
            return Json(data);
        }

        #endregion

        #region Patient Reports

        [HttpGet]
        public async Task<IActionResult> PatientReports()
        {
            var model = await _reportsService.GetPatientReportsAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PatientDetails(Guid patientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var model = await _reportsService.GetPatientDetailReportAsync(patientId, fromDate, toDate);
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPatientReport(Guid patientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var model = await _reportsService.GetPatientDetailReportAsync(patientId, fromDate, toDate);
                if (model == null)
                    return NotFound();

                var excelBytes = await GeneratePatientExcelReport(model);
                var fileName = $"Patient_Report_{model.PatientName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating Excel report.";
                return RedirectToAction("PatientReports");
            }
        }

        // ========== NEW METHOD: Export ALL Patients Report ==========
        [HttpGet]
        public async Task<IActionResult> ExportAllPatientsReport()
        {
            try
            {
                // Get all patients data
                var model = await _reportsService.GetPatientReportsAsync();

                if (model?.Patients == null || !model.Patients.Any())
                {
                    TempData["ErrorMessage"] = "No patients data available to export.";
                    return RedirectToAction("PatientReports");
                }

                // Generate comprehensive Excel report
                var excelBytes = await GenerateAllPatientsExcelReport(model);
                var fileName = $"All_Patients_Comprehensive_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating Excel report: {ex.Message}";
                return RedirectToAction("PatientReports");
            }
        }

        // ========== NEW METHOD: Generate Comprehensive ALL Patients Excel Report ==========
        private async Task<byte[]> GenerateAllPatientsExcelReport(PatientReportsViewModel model)
        {
            using var workbook = new XLWorkbook();

            // ============ SHEET 1: SUMMARY OVERVIEW ============
            var summarySheet = workbook.Worksheets.Add("📊 Summary Overview");

            // Title Section
            summarySheet.Cell(1, 1).Value = "COMPREHENSIVE PATIENTS REPORT";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 18;
            summarySheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            summarySheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(102, 126, 234);
            summarySheet.Range(1, 1, 1, 6).Merge();
            summarySheet.Range(1, 1, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            summarySheet.Cell(2, 1).Value = $"Generated on: {DateTime.Now:dddd, dd MMMM yyyy HH:mm:ss}";
            summarySheet.Cell(2, 1).Style.Font.FontSize = 11;
            summarySheet.Range(2, 1, 2, 6).Merge();
            summarySheet.Range(2, 1, 2, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Key Metrics Section
            int summaryRow = 4;
            summarySheet.Cell(summaryRow, 1).Value = "KEY METRICS";
            summarySheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            summarySheet.Cell(summaryRow, 1).Style.Font.FontSize = 14;
            summarySheet.Cell(summaryRow, 1).Style.Font.FontColor = XLColor.FromArgb(102, 126, 234);
            summaryRow += 2;

            // Metrics Table
            var metricsData = new[]
            {
                new { Label = "📋 Total Patients:", Value = model.TotalPatients.ToString(), BgColor = XLColor.FromArgb(220, 231, 255) },
                new { Label = "✅ Active Patients:", Value = model.ActivePatients.ToString(), BgColor = XLColor.FromArgb(209, 250, 229) },
                new { Label = "📅 Registered This Month:", Value = model.PatientsThisMonth.ToString(), BgColor = XLColor.FromArgb(254, 249, 195) },
                new { Label = "👨 Male Patients:", Value = model.Patients.Count(p => p.PatientGender == "Male").ToString(), BgColor = XLColor.FromArgb(191, 219, 254) },
                new { Label = "👩 Female Patients:", Value = model.Patients.Count(p => p.PatientGender == "Female").ToString(), BgColor = XLColor.FromArgb(252, 231, 243) },
                new { Label = "📊 Total Procedures:", Value = model.Patients.Sum(p => p.TotalProcedures).ToString(), BgColor = XLColor.FromArgb(221, 214, 254) }
            };

            foreach (var metric in metricsData)
            {
                summarySheet.Cell(summaryRow, 2).Value = metric.Label;
                summarySheet.Cell(summaryRow, 2).Style.Font.Bold = true;
                summarySheet.Cell(summaryRow, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

                summarySheet.Cell(summaryRow, 3).Value = metric.Value;
                summarySheet.Cell(summaryRow, 3).Style.Font.Bold = true;
                summarySheet.Cell(summaryRow, 3).Style.Font.FontSize = 13;
                summarySheet.Cell(summaryRow, 3).Style.Fill.BackgroundColor = metric.BgColor;
                summarySheet.Cell(summaryRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                summaryRow++;
            }

            // Age Distribution Section
            if (model.PatientAgeStatistics?.Any() == true)
            {
                summaryRow += 2;
                summarySheet.Cell(summaryRow, 1).Value = "AGE GROUP DISTRIBUTION";
                summarySheet.Cell(summaryRow, 1).Style.Font.Bold = true;
                summarySheet.Cell(summaryRow, 1).Style.Font.FontSize = 14;
                summarySheet.Cell(summaryRow, 1).Style.Font.FontColor = XLColor.FromArgb(102, 126, 234);
                summaryRow += 2;

                // Headers
                var ageHeaders = new[] { "Age Group", "Count", "Percentage" };
                for (int i = 0; i < ageHeaders.Length; i++)
                {
                    summarySheet.Cell(summaryRow, i + 2).Value = ageHeaders[i];
                    summarySheet.Cell(summaryRow, i + 2).Style.Font.Bold = true;
                    summarySheet.Cell(summaryRow, i + 2).Style.Fill.BackgroundColor = XLColor.FromArgb(102, 126, 234);
                    summarySheet.Cell(summaryRow, i + 2).Style.Font.FontColor = XLColor.White;
                    summarySheet.Cell(summaryRow, i + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                summaryRow++;

                // Age Data
                foreach (var ageStat in model.PatientAgeStatistics)
                {
                    summarySheet.Cell(summaryRow, 2).Value = ageStat.AgeGroup;
                    summarySheet.Cell(summaryRow, 3).Value = ageStat.Count;
                    summarySheet.Cell(summaryRow, 4).Value = $"{ageStat.Percentage:F1}%";

                    // Alternate row colors
                    var bgColor = summaryRow % 2 == 0 ? XLColor.White : XLColor.FromArgb(248, 250, 252);
                    summarySheet.Range(summaryRow, 2, summaryRow, 4).Style.Fill.BackgroundColor = bgColor;
                    summaryRow++;
                }
            }

            // Auto-fit columns
            summarySheet.ColumnsUsed().AdjustToContents();
            summarySheet.Column(1).Width = 5;

            // ============ SHEET 2: ALL PATIENTS DETAILS ============
            var detailsSheet = workbook.Worksheets.Add("📋 All Patients Details");

            // Headers
            var headers = new[]
            {
                "S.No", "MR Number", "Patient Name", "Age", "Gender",
                "Phone", "City", "Total Procedures", "Last Procedure Date",
                "Registration Date", "Status"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                detailsSheet.Cell(1, i + 1).Value = headers[i];
                detailsSheet.Cell(1, i + 1).Style.Font.Bold = true;
                detailsSheet.Cell(1, i + 1).Style.Font.FontSize = 12;
                detailsSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                detailsSheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                detailsSheet.Cell(1, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                detailsSheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                detailsSheet.Cell(1, i + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Data Rows
            int row = 2;
            int serialNo = 1;
            foreach (var patient in model.Patients.OrderBy(p => p.PatientName))
            {
                detailsSheet.Cell(row, 1).Value = serialNo++;
                detailsSheet.Cell(row, 2).Value = patient.PatientMRNumber;
                detailsSheet.Cell(row, 3).Value = patient.PatientName;
                detailsSheet.Cell(row, 4).Value = patient.PatientAge;
                detailsSheet.Cell(row, 5).Value = patient.PatientGender;
                detailsSheet.Cell(row, 6).Value = patient.PatientPhone ?? "N/A";
                detailsSheet.Cell(row, 7).Value = "N/A"; // City - you can add this from patient data
                detailsSheet.Cell(row, 8).Value = patient.TotalProcedures;
                detailsSheet.Cell(row, 9).Value = patient.LastProcedureDate.HasValue
                    ? patient.LastProcedureDate.Value.ToString("dd/MM/yyyy")
                    : "No Procedures";
                detailsSheet.Cell(row, 10).Value = patient.RegistrationDate.ToString("dd/MM/yyyy");
                detailsSheet.Cell(row, 11).Value = "Active";

                // Formatting
                detailsSheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                detailsSheet.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Gender color coding
                if (patient.PatientGender == "Male")
                    detailsSheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(191, 219, 254);
                else if (patient.PatientGender == "Female")
                    detailsSheet.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromArgb(252, 231, 243);

                // Procedure count color coding
                if (patient.TotalProcedures == 0)
                    detailsSheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromArgb(254, 226, 226);
                else if (patient.TotalProcedures >= 5)
                    detailsSheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromArgb(209, 250, 229);

                // Alternate row colors
                if (row % 2 == 0)
                    detailsSheet.Range(row, 1, row, 11).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);

                // Borders
                detailsSheet.Range(row, 1, row, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            // Summary Row
            row++;
            detailsSheet.Cell(row, 1).Value = "TOTALS:";
            detailsSheet.Cell(row, 1).Style.Font.Bold = true;
            detailsSheet.Range(row, 1, row, 7).Merge();
            detailsSheet.Cell(row, 8).Value = model.Patients.Sum(p => p.TotalProcedures);
            detailsSheet.Cell(row, 8).Style.Font.Bold = true;
            detailsSheet.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromArgb(102, 126, 234);
            detailsSheet.Cell(row, 8).Style.Font.FontColor = XLColor.White;

            // Auto-fit columns
            detailsSheet.ColumnsUsed().AdjustToContents();

            // ============ SHEET 3: PROCEDURES SUMMARY (if you want detailed procedure data) ============
            var proceduresSheet = workbook.Worksheets.Add("🔬 Procedures Summary");

            proceduresSheet.Cell(1, 1).Value = "PROCEDURES SUMMARY BY PATIENT";
            proceduresSheet.Cell(1, 1).Style.Font.Bold = true;
            proceduresSheet.Cell(1, 1).Style.Font.FontSize = 14;
            proceduresSheet.Range(1, 1, 1, 5).Merge();

            // Procedure Headers
            var procHeaders = new[] { "Patient MR", "Patient Name", "Total Procedures", "Last Procedure", "Status" };
            for (int i = 0; i < procHeaders.Length; i++)
            {
                proceduresSheet.Cell(3, i + 1).Value = procHeaders[i];
                proceduresSheet.Cell(3, i + 1).Style.Font.Bold = true;
                proceduresSheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(102, 126, 234);
                proceduresSheet.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }

            int procRow = 4;
            foreach (var patient in model.Patients.Where(p => p.TotalProcedures > 0).OrderByDescending(p => p.TotalProcedures))
            {
                proceduresSheet.Cell(procRow, 1).Value = patient.PatientMRNumber;
                proceduresSheet.Cell(procRow, 2).Value = patient.PatientName;
                proceduresSheet.Cell(procRow, 3).Value = patient.TotalProcedures;
                proceduresSheet.Cell(procRow, 4).Value = patient.LastProcedureDate?.ToString("dd/MM/yyyy") ?? "N/A";
                proceduresSheet.Cell(procRow, 5).Value = "Completed";

                procRow++;
            }

            proceduresSheet.ColumnsUsed().AdjustToContents();

            // ============ SHEET 4: STATISTICS & ANALYTICS ============
            var statsSheet = workbook.Worksheets.Add("📈 Statistics");

            statsSheet.Cell(1, 1).Value = "PATIENT STATISTICS & ANALYTICS";
            statsSheet.Cell(1, 1).Style.Font.Bold = true;
            statsSheet.Cell(1, 1).Style.Font.FontSize = 16;
            statsSheet.Range(1, 1, 1, 4).Merge();

            int statsRow = 3;

            // Gender Distribution
            statsSheet.Cell(statsRow, 1).Value = "GENDER DISTRIBUTION";
            statsSheet.Cell(statsRow, 1).Style.Font.Bold = true;
            statsRow += 2;

            var genderStats = model.Patients.GroupBy(p => p.PatientGender).Select(g => new { Gender = g.Key, Count = g.Count() });
            foreach (var stat in genderStats)
            {
                statsSheet.Cell(statsRow, 2).Value = stat.Gender;
                statsSheet.Cell(statsRow, 3).Value = stat.Count;
                statsSheet.Cell(statsRow, 4).Value = $"{(double)stat.Count / model.TotalPatients * 100:F1}%";
                statsRow++;
            }

            statsSheet.ColumnsUsed().AdjustToContents();

            // Save workbook
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        [HttpGet]
        public async Task<IActionResult> PrintPatientReport(Guid patientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var model = await _reportsService.GetPatientDetailReportAsync(patientId, fromDate, toDate);
            if (model == null)
                return NotFound();

            var htmlContent = await GeneratePatientPrintHtml(model);
            var pdfBytes = ConvertHtmlToPdf(htmlContent);

            var fileName = $"Patient_Report_{model.PatientName}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion

        #region Procedure Reports

        [HttpGet]
        public async Task<IActionResult> ProcedureReports(DateTime? fromDate = null, DateTime? toDate = null, string status = null)
        {
            var model = await _reportsService.GetProcedureReportsAsync(fromDate, toDate, status);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ProcedureAnalytics()
        {
            var model = await _reportsService.GetProcedureAnalyticsAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportProcedureReport(DateTime? fromDate = null, DateTime? toDate = null, string status = null)
        {
            try
            {
                var model = await _reportsService.GetProcedureReportsAsync(fromDate, toDate, status);
                var excelBytes = await GenerateProcedureExcelReport(model);
                var fileName = $"Procedure_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating Excel report.";
                return RedirectToAction("ProcedureReports");
            }
        }

        #endregion

        #region Inventory Reports

        [HttpGet]
        public async Task<IActionResult> InventoryReports()
        {
            var model = await _reportsService.GetInventoryReportsAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StockMovement(DateTime? fromDate = null, DateTime? toDate = null, Guid? itemId = null)
        {
            var model = await _reportsService.GetStockMovementReportAsync(fromDate, toDate, itemId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryReport()
        {
            try
            {
                var model = await _reportsService.GetInventoryReportsAsync();
                var excelBytes = await GenerateInventoryExcelReport(model);
                var fileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating Excel report.";
                return RedirectToAction("InventoryReports");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportStockMovement(DateTime? fromDate = null, DateTime? toDate = null, Guid? itemId = null)
        {
            try
            {
                var model = await _reportsService.GetStockMovementReportAsync(fromDate, toDate, itemId);
                var excelBytes = await GenerateStockMovementExcelReport(model);
                var fileName = $"Stock_Movement_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating Excel report.";
                return RedirectToAction("StockMovement");
            }
        }

        #endregion

        #region Financial Reports

        [HttpGet]
        public async Task<IActionResult> FinancialReports(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var model = await _reportsService.GetFinancialReportsAsync(fromDate, toDate);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportFinancialReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var model = await _reportsService.GetFinancialReportsAsync(fromDate, toDate);
                var excelBytes = await GenerateFinancialExcelReport(model);
                var fileName = $"Financial_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating Excel report.";
                return RedirectToAction("FinancialReports");
            }
        }

        #endregion

        #region Custom Reports

        [HttpGet]
        public IActionResult CustomReport()
        {
            return View(new CustomReportViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCustomReport(CustomReportViewModel model)
        {
            if (!ModelState.IsValid)
                return View("CustomReport", model);

            try
            {
                var reportData = await _reportsService.GenerateCustomReportAsync(model);

                if (model.ExportFormat == "Excel")
                {
                    var excelBytes = await GenerateCustomExcelReport(reportData, model);
                    var fileName = $"Custom_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (model.ExportFormat == "PDF")
                {
                    var htmlContent = await GenerateCustomPrintHtml(reportData, model);
                    var pdfBytes = ConvertHtmlToPdf(htmlContent);
                    var fileName = $"Custom_Report_{DateTime.Now:yyyyMMdd}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }

                return View("CustomReportResults", reportData);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating custom report.";
                return View("CustomReport", model);
            }
        }

        #endregion

        #region AJAX Methods

        [HttpGet]
        public async Task<JsonResult> GetPatientsForReport()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive)
                .Select(p => new {
                    value = p.PatientId,
                    text = $"{p.PatientName} ({p.PatientMRNumber})"
                })
                .OrderBy(p => p.text)
                .ToListAsync();

            return Json(patients);
        }

        [HttpGet]
        public async Task<JsonResult> GetItemsForReport()
        {
            var items = await _context.Items
                .Where(i => i.IsActive)
                .Select(i => new {
                    value = i.ItemId,
                    text = $"{i.ItemName} ({i.ItemCode})"
                })
                .OrderBy(i => i.text)
                .ToListAsync();

            return Json(items);
        }

        [HttpGet]
        public async Task<JsonResult> GetReportWidgetData(string widgetType)
        {
            var data = await _reportsService.GetWidgetDataAsync(widgetType);
            return Json(data);
        }

        #endregion

        #region Private Helper Methods - Excel Generation

        private async Task<byte[]> GeneratePatientExcelReport(PatientDetailReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Patient Report");

            // Header
            worksheet.Cell(1, 1).Value = "PATIENT DETAILED REPORT";
            worksheet.Range(1, 1, 1, 8).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 8).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Patient Info
            int row = 3;
            worksheet.Cell(row, 1).Value = "Patient Name:";
            worksheet.Cell(row, 2).Value = model.PatientName;
            worksheet.Cell(row, 3).Value = "MR Number:";
            worksheet.Cell(row, 4).Value = model.PatientMRNumber;

            row++;
            worksheet.Cell(row, 1).Value = "Phone:";
            worksheet.Cell(row, 2).Value = model.PatientPhone;
            worksheet.Cell(row, 3).Value = "Age:";
            worksheet.Cell(row, 4).Value = model.PatientAge;

            // Procedures Table
            row += 3;
            worksheet.Cell(row, 1).Value = "PROCEDURES";
            worksheet.Range(row, 1, row, 8).Style.Font.Bold = true;

            row++;
            var headers = new[] { "Date", "ROI", "Duration", "Status", "Items Used", "Created By", "Remarks" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
                worksheet.Cell(row, i + 1).Style.Font.Bold = true;
                worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            foreach (var procedure in model.Procedures)
            {
                row++;
                worksheet.Cell(row, 1).Value = procedure.ProcedureDate.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 2).Value = procedure.ProcedureROI;
                worksheet.Cell(row, 3).Value = procedure.ProcedureDuration;
                worksheet.Cell(row, 4).Value = procedure.ProcedureStatus;
                worksheet.Cell(row, 5).Value = procedure.TotalItemsUsed;
                worksheet.Cell(row, 6).Value = procedure.CreatedByName;
                worksheet.Cell(row, 7).Value = procedure.ProcedureRemarks ?? "";
            }

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GenerateProcedureExcelReport(ProcedureReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Procedure Report");

            // Headers
            var headers = new[] {
                "Date", "Patient Name", "MR Number", "ROI", "Duration",
                "Status", "Items Assigned", "Items Used", "Created By", "Remarks"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            int row = 2;
            foreach (var procedure in model.Procedures)
            {
                worksheet.Cell(row, 1).Value = procedure.ProcedureDate.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 2).Value = procedure.PatientName;
                worksheet.Cell(row, 3).Value = procedure.PatientMRNumber;
                worksheet.Cell(row, 4).Value = procedure.ProcedureROI;
                worksheet.Cell(row, 5).Value = procedure.ProcedureDuration;
                worksheet.Cell(row, 6).Value = procedure.ProcedureStatus;
                worksheet.Cell(row, 7).Value = procedure.TotalItemsAssigned;
                worksheet.Cell(row, 8).Value = procedure.TotalItemsUsed;
                worksheet.Cell(row, 9).Value = procedure.CreatedByName;
                worksheet.Cell(row, 10).Value = procedure.ProcedureRemarks ?? "";
                row++;
            }

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GenerateInventoryExcelReport(InventoryReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventory Report");

            // Summary Section
            worksheet.Cell(1, 1).Value = "INVENTORY SUMMARY";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            worksheet.Cell(2, 1).Value = "Total Items:";
            worksheet.Cell(2, 2).Value = model.TotalItems;
            worksheet.Cell(3, 1).Value = "Low Stock Items:";
            worksheet.Cell(3, 2).Value = model.LowStockItems;
            worksheet.Cell(4, 1).Value = "Out of Stock:";
            worksheet.Cell(4, 2).Value = model.OutOfStockItems;
            worksheet.Cell(5, 1).Value = "Total Value:";
            worksheet.Cell(5, 2).Value = model.TotalInventoryValue;

            // Items Table
            int row = 7;
            worksheet.Cell(row, 1).Value = "ITEM DETAILS";
            worksheet.Cell(row, 1).Style.Font.Bold = true;

            row++;
            var headers = new[] { "Item Code", "Item Name", "Category", "Current Stock", "Unit", "Reorder Level", "Status", "Value" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
                worksheet.Cell(row, i + 1).Style.Font.Bold = true;
                worksheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            foreach (var item in model.ItemStockDetails)
            {
                row++;
                worksheet.Cell(row, 1).Value = item.ItemCode;
                worksheet.Cell(row, 2).Value = item.ItemName;
                worksheet.Cell(row, 3).Value = item.Category;
                worksheet.Cell(row, 4).Value = item.CurrentStock;
                worksheet.Cell(row, 5).Value = item.Unit;
                worksheet.Cell(row, 6).Value = item.ReorderLevel;
                worksheet.Cell(row, 7).Value = item.Status;
                worksheet.Cell(row, 8).Value = item.TotalValue;

                // Highlight low stock items
                if (item.CurrentStock <= item.ReorderLevel)
                {
                    worksheet.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }
            }

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GenerateStockMovementExcelReport(StockMovementReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stock Movement");

            var headers = new[] { "Date", "Item Name", "Movement Type", "Quantity", "Unit", "Reason", "User", "Notes" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Orange;
            }

            int row = 2;
            foreach (var movement in model.StockMovements)
            {
                worksheet.Cell(row, 1).Value = movement.Date.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 2).Value = movement.ItemName;
                worksheet.Cell(row, 3).Value = movement.MovementType;
                worksheet.Cell(row, 4).Value = movement.Quantity;
                worksheet.Cell(row, 5).Value = movement.Unit;
                worksheet.Cell(row, 6).Value = movement.Reason;
                worksheet.Cell(row, 7).Value = movement.UserName;
                worksheet.Cell(row, 8).Value = movement.Notes ?? "";

                // Color coding for movement types
                if (movement.MovementType == "IN")
                    worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightGreen;
                else
                    worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.LightCoral;

                row++;
            }

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GenerateFinancialExcelReport(FinancialReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Financial Report");

            // Summary
            worksheet.Cell(1, 1).Value = "FINANCIAL SUMMARY";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            worksheet.Cell(2, 1).Value = "Total Inventory Value:";
            worksheet.Cell(2, 2).Value = model.TotalInventoryValue;
            worksheet.Cell(3, 1).Value = "Total Purchases:";
            worksheet.Cell(3, 2).Value = model.TotalPurchases;
            worksheet.Cell(4, 1).Value = "Total Consumption:";
            worksheet.Cell(4, 2).Value = model.TotalConsumptionValue;

            // Detailed breakdown would go here...

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GenerateCustomExcelReport(CustomReportResultViewModel reportData, CustomReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Custom Report");

            worksheet.Cell(1, 1).Value = model.ReportTitle ?? "Custom Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;

            // Add dynamic columns based on report data
            // This would be implemented based on your custom report structure

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion

        #region Private Helper Methods - PDF Generation

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            try
            {
                var converter = new HtmlToPdf();

                // A4 Page Settings
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.WebPageWidth = 1024;
                converter.Options.WebPageHeight = 0;

                // Margins for A4
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
                throw new Exception($"Error converting HTML to PDF: {ex.Message}");
            }
        }

        private async Task<string> GeneratePatientPrintHtml(PatientDetailReportViewModel model)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Patient Report - {model.PatientName}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; font-size: 12px; }}
        .header {{ text-align: center; border-bottom: 2px solid #333; margin-bottom: 20px; }}
        .patient-info {{ margin: 20px 0; }}
        .patient-info table {{ width: 100%; }}
        .patient-info th, .patient-info td {{ padding: 8px; text-align: left; }}
        .procedures-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .procedures-table th, .procedures-table td {{ border: 1px solid #ddd; padding: 8px; }}
        .procedures-table th {{ background-color: #f2f2f2; }}
        @media print {{ .no-print {{ display: none; }} }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Patient Detailed Report</h1>
        <p>Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    </div>
    
    <div class='patient-info'>
        <h2>Patient Information</h2>
        <table>
            <tr><th>Name:</th><td>{model.PatientName}</td><th>MR Number:</th><td>{model.PatientMRNumber}</td></tr>
            <tr><th>Phone:</th><td>{model.PatientPhone}</td><th>Age:</th><td>{model.PatientAge}</td></tr>
        </table>
    </div>
    
    <h2>Procedures History</h2>
    <table class='procedures-table'>
        <thead>
            <tr>
                <th>Date</th><th>ROI</th><th>Duration</th><th>Status</th><th>Items Used</th><th>Created By</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", model.Procedures.Select(p => $@"
            <tr>
                <td>{p.ProcedureDate:dd/MM/yyyy}</td>
                <td>{p.ProcedureROI}</td>
                <td>{p.ProcedureDuration}</td>
                <td>{p.ProcedureStatus}</td>
                <td>{p.TotalItemsUsed}</td>
                <td>{p.CreatedByName}</td>
            </tr>"))}
        </tbody>
    </table>
</body>
</html>";
        }

        private async Task<string> GenerateCustomPrintHtml(CustomReportResultViewModel reportData, CustomReportViewModel model)
        {
            // Generate HTML based on custom report structure
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{model.ReportTitle}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; font-size: 12px; }}
        .header {{ text-align: center; border-bottom: 2px solid #333; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{model.ReportTitle}</h1>
        <p>Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    </div>
    <!-- Dynamic content based on report data -->
</body>
</html>";
        }

        #endregion
    }
}
