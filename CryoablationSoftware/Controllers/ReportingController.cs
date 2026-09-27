using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.Services.Reporting;
using CryoablationSoftware.ViewModel.Reporting;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CryoablationSoftware.Controllers
{
   
        [Authorize]
        public class ReportingController : BaseController
    {
            private readonly IReportingService _reportingService;
            private readonly UserManager<IdentityUser> _userManager;
        private readonly ICurrentSiteService _currentSiteService;
        private readonly ApplicationDbContext _context;

        public ReportingController(IReportingService reportingService, UserManager<IdentityUser> userManager, ApplicationDbContext context, ICurrentSiteService currentSiteService)
        : base(currentSiteService, context)
        {
                _reportingService = reportingService;
                _userManager = userManager;
            _currentSiteService = currentSiteService;
        }

            // GET: Reporting
            [HttpGet]
            public async Task<IActionResult> Index()
            {
                var model = await _reportingService.GetReportingIndexAsync();
                return View(model);
            }

            // GET: Reporting/Generate
            [HttpGet]
            public async Task<IActionResult> Generate(string reportType = null)
            {
                var model = new ReportGenerationViewModel();

                if (!string.IsNullOrEmpty(reportType))
                {
                    model.ReportType = reportType;
                    model.ReportTitle = GetDefaultReportTitle(reportType);
                }

                await LoadReportGenerationViewData(model);
                return View(model);
            }

            // POST: Reporting/Generate
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Generate(ReportGenerationViewModel model)
            {
                if (!ModelState.IsValid)
                {
                    await LoadReportGenerationViewData(model);
                    return View(model);
                }

                try
                {
                    // Validate report parameters
                    var validation = await _reportingService.ValidateReportParametersAsync(model);
                    if (!validation.IsValid)
                    {
                        foreach (var error in validation.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        await LoadReportGenerationViewData(model);
                        return View(model);
                    }

                    var userId = _userManager.GetUserId(User);
                    var report = await _reportingService.GenerateReportAsync(model, userId);

                    if (model.OutputFormat == "PDF")
                    {
                        var pdfBytes = await _reportingService.ExportReportToPdfAsync(report);
                        return File(pdfBytes, "application/pdf", $"{model.ReportTitle}_{DateTime.Now:yyyyMMdd}.pdf");
                    }
                    else if (model.OutputFormat == "Excel")
                    {
                        var excelBytes = await _reportingService.ExportReportToExcelAsync(report);
                        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{model.ReportTitle}_{DateTime.Now:yyyyMMdd}.xlsx");
                    }
                    else
                    {
                        // Show report in browser
                        return View("ViewReport", report);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
                    await LoadReportGenerationViewData(model);
                    return View(model);
                }
            }

            // GET: Reporting/PatientProcedure/{id}
            [HttpGet]
            public async Task<IActionResult> PatientProcedure(Guid id, DateTime? fromDate = null, DateTime? toDate = null)
            {
                var report = await _reportingService.GeneratePatientProcedureReportAsync(id, fromDate, toDate);
                if (report == null)
                    return NotFound();

                return View(report);
            }

            // GET: Reporting/InventoryConsumption
            [HttpGet]
            public async Task<IActionResult> InventoryConsumption(DateTime? fromDate = null, DateTime? toDate = null, string categories = null)
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-1);
                var to = toDate ?? DateTime.Now;
                var categoryList = !string.IsNullOrEmpty(categories) ? categories.Split(',').ToList() : null;

                var report = await _reportingService.GenerateInventoryConsumptionReportAsync(from, to, categoryList);
                return View(report);
            }

            // GET: Reporting/FinancialSummary
            [HttpGet]
            public async Task<IActionResult> FinancialSummary(DateTime? fromDate = null, DateTime? toDate = null)
            {
                var from = fromDate ?? DateTime.Now.AddMonths(-3);
                var to = toDate ?? DateTime.Now;

                var report = await _reportingService.GenerateFinancialSummaryReportAsync(from, to);
                return View(report);
            }

            // POST: Reporting/ExportPatientProcedure
            [HttpPost]
            public async Task<IActionResult> ExportPatientProcedure(Guid patientId, DateTime? fromDate = null, DateTime? toDate = null, string format = "PDF")
            {
                var report = await _reportingService.GeneratePatientProcedureReportAsync(patientId, fromDate, toDate);
                if (report == null)
                    return NotFound();

                var generatedReport = new GeneratedReportViewModel
                {
                    ReportTitle = $"Patient Procedure Report - {report.PatientName}",
                    ReportType = "PatientProcedure",
                    GeneratedDate = DateTime.Now,
                    ReportData = report
                };

                if (format.ToUpper() == "EXCEL")
                {
                    var excelBytes = await _reportingService.ExportReportToExcelAsync(generatedReport);
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Patient_Report_{report.PatientName}_{DateTime.Now:yyyyMMdd}.xlsx");
                }
                else
                {
                    var pdfBytes = await _reportingService.ExportReportToPdfAsync(generatedReport);
                    return File(pdfBytes, "application/pdf",
                        $"Patient_Report_{report.PatientName}_{DateTime.Now:yyyyMMdd}.pdf");
                }
            }

            // AJAX Methods
            [HttpGet]
            public async Task<JsonResult> GetReportTemplates()
            {
                var templates = await _reportingService.GetReportTemplatesAsync();
                return Json(templates);
            }

            [HttpGet]
            public async Task<JsonResult> GetPatientsForReport()
            {
                var patients = await _reportingService.GetPatientsForReportAsync();
                return Json(patients);
            }

            [HttpGet]
            public async Task<JsonResult> GetItemsForReport()
            {
                var items = await _reportingService.GetItemsForReportAsync();
                return Json(items);
            }

            [HttpGet]
            public async Task<JsonResult> GetCategoriesForReport()
            {
                var categories = await _reportingService.GetCategoriesForReportAsync();
                return Json(categories);
            }

            // Private Helper Methods
            private async Task LoadReportGenerationViewData(ReportGenerationViewModel model)
            {
                ViewBag.Patients = await _reportingService.GetPatientsForReportAsync();
                ViewBag.Items = await _reportingService.GetItemsForReportAsync();
                ViewBag.Categories = await _reportingService.GetCategoriesForReportAsync();
                ViewBag.ReportTemplates = await _reportingService.GetReportTemplatesAsync();

                ViewBag.OutputFormats = new List<SelectListItem>
            {
                new() { Value = "PDF", Text = "PDF Document" },
                new() { Value = "Excel", Text = "Excel Spreadsheet" },
                new() { Value = "View", Text = "View in Browser" }
            };

                ViewBag.PageSizes = new List<string> { "A4", "A3", "Letter", "Legal" };
                ViewBag.Orientations = new List<string> { "Portrait", "Landscape" };
            }

            private string GetDefaultReportTitle(string reportType)
            {
                return reportType switch
                {
                    "PatientProcedure" => "Patient Procedure Report",
                    "InventoryConsumption" => "Inventory Consumption Report",
                    "FinancialSummary" => "Financial Summary Report",
                    "StockMovement" => "Stock Movement Report",
                    "ProcedureAnalysis" => "Procedure Analysis Report",
                    _ => "Custom Report"
                };
            }
        }

        public class SelectListItem
        {
            public string Value { get; set; }
            public string Text { get; set; }
            public bool Selected { get; set; }
        }
    }

