using CryoablationSoftware.Constants;
using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SelectPdf;
using static CryoablationSoftware.ViewModel.ProcedureEditViewModel;

namespace CryoablationSoftware.Controllers
{
    [Authorize]
    public class ProcedureController : Controller
    {
        private readonly IProcedureService _procedureService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IInventoryService _inventoryService;

        public ProcedureController(IProcedureService procedureService, UserManager<IdentityUser> userManager,   ApplicationDbContext context, IAuditLogService auditLogService, IInventoryService inventoryService)
        {
            _procedureService = procedureService;
            _userManager = userManager;
            _context = context;
            _auditLogService = auditLogService;
            _inventoryService = inventoryService;
        }

        #region Dashboard & Main Views

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _procedureService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        public async Task<IActionResult> Procedures()
        {
            var procedures = await _procedureService.GetAllProceduresAsync();
            return View(procedures);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var procedure = await _procedureService.GetProcedureDetailsAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            return View(procedure);
        }

        // Add this method in ProcedureController class

        [HttpGet]
        public async Task<IActionResult> ByStatuses(string status)
        {
            // Validate status
            var validStatuses = new[] { "Scheduled", "In-Progress", "Completed", "Cancelled" };
            if (string.IsNullOrEmpty(status) || !validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status provided";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var procedures = await _procedureService.GetProceduresByStatusesAsync(status);
                ViewBag.StatusFilter = status;
                ViewBag.StatusCount = procedures.Count();

                return View(procedures);
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    actionType: "GET_BY_STATUS_FAILED",
                    message: $"Failed to get procedures by status: {status}",
                    tableName: "Procedures",
                    additionalData: new { Status = status, Error = ex.Message },
                    serviceName: "ProcedureController",
                    methodName: "ByStatus"
                );

                TempData["ErrorMessage"] = "Error loading procedures";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Create Procedure

        [HttpGet]
        public async Task<IActionResult> Create(Guid? patientId)
        {
            await LoadCreateViewData();

            var model = new ProcedureCreateViewModel();

            // Set default date to tomorrow at 9 AM if not specified
            model.ProcedureDate = DateTime.Now.Date.AddDays(1).AddHours(9);
            model.ProcedureStatus = "Scheduled"; // Set default status

            if (patientId.HasValue)
            {
                model.PatientId = patientId.Value;
                // Load patient name for display
                var patients = await _procedureService.GetActivePatientsAsync();
                var patient = patients.FirstOrDefault(p => p.PatientId == patientId.Value);
                model.PatientName = patient?.PatientName;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProcedureCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var result = await _procedureService.CreateProcedureAsync(model, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Procedure created successfully";
                    return RedirectToAction(nameof(Procedures));
                }
                else
                {
                    // Log creation failure
                    await _auditLogService.LogAsync(
                        actionType: "CREATE_FAILED",
                        message: "Procedure creation failed in service",
                        tableName: "Procedures",
                        additionalData: new { PatientId = model.PatientId },
                        serviceName: "ProcedureController",
                        methodName: "Create"
                    );

                    TempData["ErrorMessage"] = "Error creating procedure. Please check item availability.";
                }
            }
            else
            {
                // Add detailed error messages
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Procedure creation validation failed",
                    tableName: "Procedures",
                    additionalData: new { PatientId = model.PatientId, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() },
                    serviceName: "ProcedureController",
                    methodName: "Create"
                );

                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
            }

            // IMPORTANT: Reload ViewBag data when returning to view with errors
            await LoadCreateViewData();

            // Ensure patient name is loaded for display even on error
            if (model.PatientId != Guid.Empty)
            {
                var patients = await _procedureService.GetActivePatientsAsync();
                var patient = patients.FirstOrDefault(p => p.PatientId == model.PatientId);
                model.PatientName = patient?.PatientName;
            }

            return View(model);
        }
        #endregion

        #region Edit Procedure

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var procedure = await _procedureService.GetProcedureForEditAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            var model = new ProcedureEditViewModel
            {
                ProcedureId = procedure.ProcedureId,
                PatientId = procedure.PatientId,
                PatientName = procedure.Patient?.PatientName,
                ProcedureDate = procedure.ProcedureDate,
                ProcedureROI = procedure.ProcedureROI,
                ProcedureDuration = procedure.ProcedureDuration,
                ProcedureEquipmentUsed = procedure.ProcedureEquipmentUsed,
                ProcedureRemarks = procedure.ProcedureRemarks,
                ProcedureStatus = procedure.ProcedureStatus,
                ProcedureNextDate = procedure.ProcedureNextDate,
                PreviousProcedureId = procedure.PreviousProcedureId,
                PrimaryDoctorName = procedure.PrimaryDoctorName,
                ConsultantDoctor1Name = procedure.ConsultantDoctor1Name,
                ConsultantDoctor2Name = procedure.ConsultantDoctor2Name,
                OperatingEngineerName = procedure.OperatingEngineerName,
                IsActive = procedure.IsActive,

                // Medical Record Data
                MedicalRecordCancerType = !string.IsNullOrEmpty(procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordCancerType)
                    ? procedure.Patient.MedicalRecords.FirstOrDefault().MedicalRecordCancerType.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                MedicalRecordCancerStage = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordCancerStage,
                MedicalRecordComorbidities = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordComorbidities,
                MedicalRecordTumorLocation = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordTumorLocation,
                MedicalRecordAllergies = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordAllergies,
                MedicalRecordClinicalHistory = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.MedicalRecordClinicalHistory,

                // Board Approval Data
                //BoardApprovalDate = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.BoardApproval?.BoardApprovalDate,
                //BoardApprovalDecision = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.BoardApproval?.BoardApprovalDecision,
                //BoardApprovalLetterNumber = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.BoardApproval?.BoardApprovalLetterNumber,
                //BoardApprovalAttachmentPath = procedure.Patient?.MedicalRecords?.FirstOrDefault()?.BoardApproval?.BoardApprovalAttachmentPath,

                // Clinical Reports Data
                ClinicalReports = procedure.ClinicalReports?.Where(cr => cr.IsActive)?.Select(cr => new CryoablationSoftware.ViewModel.ProcedureEditViewModel.ClinicalReportEditViewModel
                {
                    ReportType = cr.ClinicalReport.ReportType,
                    ExistingFilePath = cr.ClinicalReport.ReportFilePath
                }).ToList() ?? new List<CryoablationSoftware.ViewModel.ProcedureEditViewModel.ClinicalReportEditViewModel>()
            };

            await LoadEditViewData();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProcedureEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var result = await _procedureService.UpdateProcedureAsync(model, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Procedure updated successfully";
                    return RedirectToAction(nameof(Details), new { id = model.ProcedureId });
                }
                else
                {
                    // Log update failure
                    await _auditLogService.LogAsync(
                        actionType: "UPDATE_FAILED",
                        message: "Procedure update failed in service",
                        recordId: model.ProcedureId.ToString(),
                        tableName: "Procedures",
                        additionalData: new { ProcedureId = model.ProcedureId },
                        serviceName: "ProcedureController",
                        methodName: "Edit"
                    );

                    TempData["ErrorMessage"] = "Error updating procedure. Please try again.";
                }
            }
            else
            {
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Procedure edit validation failed",
                    recordId: model.ProcedureId.ToString(),
                    tableName: "Procedures",
                    additionalData: new { ProcedureId = model.ProcedureId, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() },
                    serviceName: "ProcedureController",
                    methodName: "Edit"
                );

                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
            }

            await LoadEditViewData();
            return View(model);
        }


        // ============ YEH NAYA METHOD ADD KARO ============
        [HttpGet]
        public async Task<IActionResult> GetPatientMedicalData(Guid patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.MedicalRecords.Where(mr => mr.IsActive))
                    .FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive);

                if (patient == null)
                    return Json(new { success = false, message = "Patient not found" });

                var medicalRecord = patient.MedicalRecords?.FirstOrDefault();

                // Get clinical reports separately
                var clinicalReports = await _context.ClinicalReports
                    .Where(cr => cr.PatientId == patientId && cr.IsActive)
                    .OrderByDescending(cr => cr.ReportDate)
                    .Select(cr => new
                    {
                        reportId = cr.ClinicalReportId,
                        reportType = cr.ReportType,
                        reportDescription = cr.ReportDescription,
                        reportDate = cr.ReportDate.ToString("yyyy-MM-dd"),
                        reportFilePath = cr.ReportFilePath
                    })
                    .ToListAsync();

                var response = new
                {
                    success = true,
                    medicalRecord = medicalRecord == null ? null : new
                    {
                        cancerType = !string.IsNullOrEmpty(medicalRecord.MedicalRecordCancerType)
                            ? medicalRecord.MedicalRecordCancerType.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                            : new List<string>(),
                        cancerStage = medicalRecord.MedicalRecordCancerStage,
                        comorbidities = medicalRecord.MedicalRecordComorbidities,
                        tumorLocation = medicalRecord.MedicalRecordTumorLocation,
                        allergies = medicalRecord.MedicalRecordAllergies,
                        clinicalHistory = medicalRecord.MedicalRecordClinicalHistory
                    },
                    clinicalReports = clinicalReports
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading patient data" });
            }
        }

        #endregion

        #region Delete Procedure

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _procedureService.DeleteProcedureAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Procedure deleted successfully";
            }
            else
            {
                // Log deletion failure
                await _auditLogService.LogAsync(
                    actionType: "DELETE_FAILED",
                    message: "Procedure deletion failed",
                    recordId: id.ToString(),
                    tableName: "Procedures",
                    additionalData: new { ProcedureId = id },
                    serviceName: "ProcedureController",
                    methodName: "Delete"
                );

                TempData["ErrorMessage"] = "Error deleting procedure";
            }

            return RedirectToAction(nameof(Procedures));
        }

        #endregion

        #region Item Assignment

        [HttpGet]
        public async Task<IActionResult> AssignItems(Guid id)
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            var items = await _procedureService.GetAvailableItemsAsync();

            ViewBag.AvailableItems = items.Select(i => new {
                Value = i.ItemId,
                Text = $"{i.ItemName} ({i.ItemCode})",
                Unit = i.Unit
            }).ToList();

            ViewBag.ProcedureId = id;
            ViewBag.PatientName = procedure.Patient?.PatientName;
            ViewBag.ProcedureROI = procedure.ProcedureROI;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMultipleItems(Guid procedureId, List<AssignItemViewModel> items, string overallNotes)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"ProcedureId: {procedureId}");
                Console.WriteLine($"Items Count: {items?.Count ?? 0}");
                Console.WriteLine($"OverallNotes: {overallNotes}");

                // Validate input
                if (items == null || !items.Any())
                {
                    TempData["ErrorMessage"] = "Please add at least one item to assign";
                    return RedirectToAction(nameof(AssignItems), new { id = procedureId });
                }

                // Validate each item has required fields
                var validationErrors = new List<string>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].ItemId == Guid.Empty)
                        validationErrors.Add($"Row {i + 1}: Item is required");

                    if (items[i].Quantity <= 0)
                        validationErrors.Add($"Row {i + 1}: Quantity must be greater than 0");

                    if (string.IsNullOrWhiteSpace(items[i].Reason))
                        validationErrors.Add($"Row {i + 1}: Reason is required");
                }

                if (validationErrors.Any())
                {
                    TempData["ErrorMessage"] = string.Join("<br/>", validationErrors);
                    return RedirectToAction(nameof(AssignItems), new { id = procedureId });
                }

                var userId = _userManager.GetUserId(User);

                // Call service method
                var result = await _procedureService.AssignMultipleItemsToProcedureAsync(
                    procedureId,
                    items,
                    overallNotes,
                    userId
                );

                // Handle result
                if (result.Success)
                {
                    if (result.SuccessCount == items.Count)
                    {
                        // All items assigned successfully
                        TempData["SuccessMessage"] = $"✓ All {result.SuccessCount} item(s) assigned successfully!";
                    }
                    else
                    {
                        // Partial success
                        var successMsg = $"✓ {result.SuccessCount} item(s) assigned successfully";
                        var errorMsg = $"✗ {result.FailCount} item(s) failed";
                        TempData["WarningMessage"] = $"{successMsg}<br/>{errorMsg}";

                        // Show detailed errors if any
                        if (result.ErrorMessages.Any())
                        {
                            TempData["ErrorDetails"] = string.Join("<br/>", result.ErrorMessages);
                        }
                    }

                    // Log successful assignment
                    await _auditLogService.LogAsync(
                        actionType: "ASSIGN_MULTIPLE_ITEMS",
                        message: $"Multiple items assigned to procedure",
                        recordId: procedureId.ToString(),
                        tableName: "ProcedureItemAssignments",
                        additionalData: new
                        {
                            ProcedureId = procedureId,
                            TotalItems = items.Count,
                            SuccessCount = result.SuccessCount,
                            FailCount = result.FailCount
                        },
                        serviceName: "ProcedureController",
                        methodName: "AssignMultipleItems"
                    );
                }
                else
                {
                    // Complete failure
                    TempData["ErrorMessage"] = "Failed to assign items:<br/>" + string.Join("<br/>", result.ErrorMessages);

                    // Log failure
                    await _auditLogService.LogAsync(
                        actionType: "ASSIGN_MULTIPLE_ITEMS_FAILED",
                        message: "Failed to assign multiple items",
                        recordId: procedureId.ToString(),
                        tableName: "ProcedureItemAssignments",
                        additionalData: new
                        {
                            ProcedureId = procedureId,
                            TotalItems = items.Count,
                            Errors = result.ErrorMessages
                        },
                        serviceName: "ProcedureController",
                        methodName: "AssignMultipleItems"
                    );
                }

                return RedirectToAction(nameof(Details), new { id = procedureId });
            }
            catch (Exception ex)
            {
                // Log unexpected exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "AssignMultipleItems",
                    customMessage: "Unexpected error during multiple item assignment",
                    additionalData: new { ProcedureId = procedureId, ItemCount = items?.Count ?? 0 },
                    serviceName: "ProcedureController"
                );

                TempData["ErrorMessage"] = "An unexpected error occurred while assigning items. Please try again.";
                return RedirectToAction(nameof(AssignItems), new { id = procedureId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAssignment(Guid assignmentId, Guid procedureId)
        {
            var result = await _procedureService.RemoveItemAssignmentAsync(assignmentId);

            if (result)
            {
                TempData["SuccessMessage"] = "Item assignment removed successfully";
            }
            else
            {
                // Log remove assignment failure
                await _auditLogService.LogAsync(
                    actionType: "REMOVE_ASSIGNMENT_FAILED",
                    message: "Failed to remove item assignment",
                    recordId: procedureId.ToString(),
                    tableName: "ProcedureItemAssignments",
                    additionalData: new { AssignmentId = assignmentId, ProcedureId = procedureId },
                    serviceName: "ProcedureController",
                    methodName: "RemoveAssignment"
                );

                TempData["ErrorMessage"] = "Cannot remove assignment - items may have been used";
            }

            return RedirectToAction(nameof(Details), new { id = procedureId });
        }
        [HttpGet]
        public async Task<JsonResult> GetItemStocknew(Guid itemId)
        {
            try
            {
                var currentStock = await _inventoryService.GetCurrentStockAsync(itemId);
                return Json(new { stock = currentStock });
            }
            catch (Exception ex)
            {
                return Json(new { stock = 0, error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditAssignedItems(Guid id)
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            if (procedure.ProcedureStatus == "Completed" || procedure.ProcedureStatus == "Cancelled")
            {
                TempData["ErrorMessage"] = $"Cannot edit items for {procedure.ProcedureStatus} procedure";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Get assigned items
            var assignedItems = await _procedureService.GetAssignedItemsAsync(id);

            // Get assigned item IDs
            var assignedItemIds = assignedItems.Select(a => a.ItemId).ToList();

            // Get ALL items - both active AND currently assigned ones (even if inactive/out of stock)
            var allItems = await _context.Items
                .Where(i => i.IsActive || assignedItemIds.Contains(i.ItemId))
                .ToListAsync();

            var model = new EditAssignedItemsPageViewModel
            {
                ProcedureId = id,
                PatientName = procedure.Patient?.PatientName ?? "",
                ProcedureROI = procedure.ProcedureROI ?? "",
                ProcedureStatus = procedure.ProcedureStatus ?? "",

                AvailableItems = allItems
                    .Select(i => new ItemDropdownViewModel
                    {
                        Value = i.ItemId.ToString(),
                        Text = $"{i.ItemName} ({i.ItemCode})",
                        Unit = i.Unit ?? ""
                    })
                    .OrderBy(i => i.Text)
                    .ToList(),

                ExistingItems = assignedItems
                    .Select(a => new ExistingAssignedItemViewModel
                    {
                        AssignmentId = a.AssignmentId.ToString(),
                        ItemId = a.ItemId.ToString(),
                        ItemName = a.Item?.ItemName ?? "",
                        ItemCode = a.Item?.ItemCode ?? "",
                        Quantity = a.QuantityAssigned,
                        Reason = a.AssignmentReason ?? "",
                        Unit = a.Item?.Unit ?? ""
                    })
                    .ToList()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignedItems(Guid procedureId, List<EditAssignItemViewModel> items, string overallNotes)
        {
            try
            {
                var procedure = await _procedureService.GetProcedureByIdAsync(procedureId);
                if (procedure == null)
                {
                    TempData["ErrorMessage"] = "Procedure not found";
                    return RedirectToAction(nameof(Details), new { id = procedureId });
                }

                if (procedure.ProcedureStatus == "Completed" || procedure.ProcedureStatus == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"Cannot edit items for {procedure.ProcedureStatus} procedure";
                    return RedirectToAction(nameof(Details), new { id = procedureId });
                }

                var activeItems = items?.Where(i => !i.IsDeleted).ToList();

                if (activeItems == null || !activeItems.Any())
                {
                    var hasDeletedItems = items?.Any(i => i.IsDeleted && i.AssignmentId.HasValue) ?? false;
                    if (!hasDeletedItems)
                    {
                        TempData["ErrorMessage"] = "Please add at least one item";
                        return RedirectToAction(nameof(EditAssignedItems), new { id = procedureId });
                    }
                }

                var validationErrors = new List<string>();
                if (activeItems != null && activeItems.Any())
                {
                    for (int i = 0; i < activeItems.Count; i++)
                    {
                        if (activeItems[i].ItemId == Guid.Empty)
                            validationErrors.Add($"Row {i + 1}: Item is required");
                        if (activeItems[i].Quantity <= 0)
                            validationErrors.Add($"Row {i + 1}: Quantity must be greater than 0");
                        if (string.IsNullOrWhiteSpace(activeItems[i].Reason))
                            validationErrors.Add($"Row {i + 1}: Reason is required");
                    }
                }

                if (validationErrors.Any())
                {
                    TempData["ErrorMessage"] = string.Join("<br/>", validationErrors);
                    return RedirectToAction(nameof(EditAssignedItems), new { id = procedureId });
                }

                var userId = _userManager.GetUserId(User);
                var result = await _procedureService.UpdateAssignedItemsAsync(procedureId, items, overallNotes, userId);

                if (result.Success)
                {
                    var totalActiveItems = activeItems?.Count ?? 0;
                    var deletedCount = items?.Count(i => i.IsDeleted) ?? 0;

                    if (result.SuccessCount == (totalActiveItems + deletedCount))
                    {
                        var message = $"✓ Changes saved successfully!";
                        if (deletedCount > 0)
                            message += $" ({deletedCount} item(s) removed)";
                        TempData["SuccessMessage"] = message;
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"✓ {result.SuccessCount} operation(s) successful<br/>✗ {result.FailCount} operation(s) failed";
                        if (result.ErrorMessages.Any())
                            TempData["ErrorDetails"] = string.Join("<br/>", result.ErrorMessages);
                    }

                    await _auditLogService.LogAsync("UPDATE_ASSIGNED_ITEMS", "Updated assigned items", procedureId.ToString(),
                        "ProcedureItemAssignments", new { ProcedureId = procedureId, SuccessCount = result.SuccessCount },
                        "ProcedureController", "UpdateAssignedItems");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update items:<br/>" + string.Join("<br/>", result.ErrorMessages);
                }

                return RedirectToAction(nameof(Details), new { id = procedureId });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogExceptionAsync(ex, "UpdateAssignedItems", "Error updating items",
                    new { ProcedureId = procedureId }, "ProcedureController");

                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction(nameof(EditAssignedItems), new { id = procedureId });
            }
        }

        #endregion

        #region Item Usage

        [HttpGet]
        public async Task<IActionResult> RecordUsage(Guid procedureId, Guid? itemId)
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(procedureId);
            if (procedure == null)
            {
                return NotFound();
            }

            var itemSummaries = await _procedureService.GetProcedureItemSummaryAsync(procedureId);

            var model = new ProcedureItemUsageViewModel
            {
                ProcedureId = procedureId,
                PatientName = procedure.Patient?.PatientName,
                ProcedureROI = procedure.ProcedureROI,
                ProcedureDate = procedure.ProcedureDate
            };

            if (itemId.HasValue)
            {
                // FIXED: Changed 'is' to 'item' in lambda expression
                var itemSummary = itemSummaries.FirstOrDefault(item => item.ItemId == itemId.Value);
                if (itemSummary != null)
                {
                    model.ItemId = itemId.Value;
                    model.ItemName = itemSummary.ItemName;
                    model.ItemCode = itemSummary.ItemCode;
                    model.Unit = itemSummary.Unit;
                    model.QuantityAssigned = itemSummary.QuantityAssigned;
                    model.PreviouslyUsed = itemSummary.QuantityUsed;
                }
            }

            // FIXED: Changed 'is' to 'item' in lambda expressions
            ViewBag.AssignedItems = itemSummaries.Where(item => item.RemainingQuantity > 0).Select(item => new {
                Value = item.ItemId,
                Text = $"{item.ItemName} ({item.ItemCode}) - Remaining: {item.RemainingQuantity} {item.Unit}",
                QuantityAssigned = item.QuantityAssigned,
                PreviouslyUsed = item.QuantityUsed,
                Unit = item.Unit,
                ItemName = item.ItemName,
                ItemCode = item.ItemCode
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordUsage(ProcedureItemUsageViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var result = await _procedureService.RecordItemUsageAsync(model, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Item usage recorded successfully";
                    return RedirectToAction(nameof(Details), new { id = model.ProcedureId });
                }
                else
                {
                    // Log usage recording failure
                    await _auditLogService.LogAsync(
                        actionType: "RECORD_USAGE_FAILED",
                        message: "Failed to record item usage",
                        recordId: model.ProcedureId.ToString(),
                        tableName: "ProcedureItemUsages",
                        additionalData: new { ProcedureId = model.ProcedureId, ItemId = model.ItemId, Quantity = model.QuantityUsed },
                        serviceName: "ProcedureController",
                        methodName: "RecordUsage"
                    );

                    TempData["ErrorMessage"] = "Error recording usage - check stock availability or assignment limits";
                }
            }

            // Reload view data if validation fails
            var procedure = await _procedureService.GetProcedureByIdAsync(model.ProcedureId);
            var itemSummaries = await _procedureService.GetProcedureItemSummaryAsync(model.ProcedureId);

            model.PatientName = procedure?.Patient?.PatientName;
            model.ProcedureROI = procedure?.ProcedureROI;
            model.ProcedureDate = procedure?.ProcedureDate ?? DateTime.MinValue;

            // FIXED: Changed 'is' to 'item' in lambda expressions
            ViewBag.AssignedItems = itemSummaries.Where(item => item.RemainingQuantity > 0).Select(item => new {
                Value = item.ItemId,
                Text = $"{item.ItemName} ({item.ItemCode}) - Remaining: {item.RemainingQuantity} {item.Unit}",
                QuantityAssigned = item.QuantityAssigned,
                PreviouslyUsed = item.QuantityUsed,
                Unit = item.Unit,
                ItemName = item.ItemName,
                ItemCode = item.ItemCode
            }).ToList();

            return View(model);
        }

        #endregion

        #region Clinical Reports

        [HttpGet]
        public async Task<IActionResult> LinkReport(Guid procedureId)
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(procedureId);
            if (procedure == null)
            {
                return NotFound();
            }

            var availableReports = await _procedureService.GetAvailableClinicalReportsAsync(procedure.PatientId, procedureId);

            var model = new AddClinicalReportViewModel
            {
                ProcedureId = procedureId,
                PatientName = procedure.Patient?.PatientName,
                ProcedureROI = procedure.ProcedureROI,
                ProcedureDate = procedure.ProcedureDate,
                AvailableReports = availableReports
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkReport(AddClinicalReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var result = await _procedureService.LinkClinicalReportAsync(model.ProcedureId, model.ClinicalReportId, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Clinical report linked successfully";
                    return RedirectToAction(nameof(Details), new { id = model.ProcedureId });
                }
                else
                {
                    // Log link report failure
                    await _auditLogService.LogAsync(
                        actionType: "LINK_REPORT_FAILED",
                        message: "Failed to link clinical report",
                        recordId: model.ProcedureId.ToString(),
                        tableName: "ProcedureClinicalReports",
                        additionalData: new { ProcedureId = model.ProcedureId, ClinicalReportId = model.ClinicalReportId },
                        serviceName: "ProcedureController",
                        methodName: "LinkReport"
                    );

                    TempData["ErrorMessage"] = "Error linking report - may already be linked";
                }
            }

            // Reload available reports
            var procedure = await _procedureService.GetProcedureByIdAsync(model.ProcedureId);
            if (procedure != null)
            {
                model.AvailableReports = await _procedureService.GetAvailableClinicalReportsAsync(procedure.PatientId, model.ProcedureId);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UnlinkReport(Guid procedureId, Guid clinicalReportId)
        {
            var result = await _procedureService.UnlinkClinicalReportAsync(procedureId, clinicalReportId);

            if (result)
            {
                TempData["SuccessMessage"] = "Clinical report unlinked successfully";
            }
            else
            {
                // Log unlink report failure
                await _auditLogService.LogAsync(
                    actionType: "UNLINK_REPORT_FAILED",
                    message: "Failed to unlink clinical report",
                    recordId: procedureId.ToString(),
                    tableName: "ProcedureClinicalReports",
                    additionalData: new { ProcedureId = procedureId, ClinicalReportId = clinicalReportId },
                    serviceName: "ProcedureController",
                    methodName: "UnlinkReport"
                );

                TempData["ErrorMessage"] = "Error unlinking report";
            }

            return RedirectToAction(nameof(Details), new { id = procedureId });
        }

        #endregion

        #region Filter Views

        public async Task<IActionResult> ByPatient(Guid patientId)
        {
            var procedures = await _procedureService.GetProceduresByPatientAsync(patientId);

            var patients = await _procedureService.GetActivePatientsAsync();
            var patient = patients.FirstOrDefault(p => p.PatientId == patientId);
            ViewBag.PatientName = patient?.PatientName;
            ViewBag.PatientId = patientId;

            return View("ProceduresByFilter", procedures);
        }

        public async Task<IActionResult> ByStatus(string status)
        {
            var procedures = await _procedureService.GetProceduresByStatusAsync(status);
            ViewBag.FilterTitle = $"Procedures - {status}";
            ViewBag.FilterValue = status;

            return View("ProceduresByFilter", procedures);
        }

        #endregion

        #region Generate PDF Report - SelectPdf .NET Core
        [HttpGet]
        public async Task<IActionResult> GenerateReport(Guid id)
        {
            try
            {
                var procedureDetails = await _procedureService.GetProcedureDetailsAsync(id);
                if (procedureDetails == null)
                {
                    TempData["ErrorMessage"] = "Procedure not found.";
                    return RedirectToAction("Procedures");
                }

                // Generate PDF using QuestPDF
                var pdfBytes = GenerateQuestPdf(procedureDetails);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "Error generating PDF.";
                    return RedirectToAction("Details", new { id });
                }

                var fileName = $"Procedure_Report_{SanitizeFileName(procedureDetails.PatientName)}_{procedureDetails.ProcedureDate:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }
        private byte[] GenerateQuestPdf(ProcedureDetailsViewModel model)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, model));
                        page.Content().Element(c => ComposeContent(c, model));

                        // Footer
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.Span($" | Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QuestPDF Error: {ex.Message}");
                throw;
            }
        }

        private void ComposeHeader(IContainer container, ProcedureDetailsViewModel model)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(2).BorderColor(Colors.Blue.Medium).PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("🏥 CryoablationSoftware")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Blue.Medium);

                        col.Item().Text("MEDICAL PROCEDURE REPORT")
                            .FontSize(14)
                            .SemiBold()
                            .FontColor(Colors.Black);

                        col.Item().Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        private void ComposeContent(IContainer container, ProcedureDetailsViewModel model)
        {
            container.PaddingVertical(15).Column(column =>
            {
                column.Spacing(12);

                // Patient Information
                column.Item().Element(c => ComposeSectionTitle(c, "PATIENT INFORMATION"));
                column.Item().Element(c => ComposePatientInfo(c, model));

                // Procedure Information
                column.Item().Element(c => ComposeSectionTitle(c, "PROCEDURE INFORMATION"));
                column.Item().Element(c => ComposeProcedureInfo(c, model));

                // Procedure Notes
                if (!string.IsNullOrEmpty(model.ProcedureRemarks))
                {
                    column.Item().Element(c => ComposeSectionTitle(c, "PROCEDURE NOTES"));
                    column.Item().Background("#f8f9fa")
                        .Padding(10)
                        .Text(model.ProcedureRemarks)
                        .FontSize(10);
                }

                // Medical Record
                if (model.HasMedicalRecord)
                {
                    column.Item().Element(c => ComposeSectionTitle(c, "MEDICAL RECORD"));
                    column.Item().Element(c => ComposeMedicalRecord(c, model));
                }

                // Items Used
                if (model.ItemSummaries?.Any() == true)
                {
                    column.Item().Element(c => ComposeSectionTitle(c, "ITEMS USED"));
                    column.Item().Element(c => ComposeItemsTable(c, model.ItemSummaries));
                }

                // Clinical Reports
                if (model.ClinicalReports?.Any() == true)
                {
                    column.Item().Element(c => ComposeSectionTitle(c, "CLINICAL REPORTS"));
                    column.Item().Element(c => ComposeClinicalReportsTable(c, model.ClinicalReports));
                }
            });
        }

        private void ComposeSectionTitle(IContainer container, string title)
        {
            container.Background("#e3f2fd")
                .Padding(8)
                .Text(title)
                .FontSize(12)
                .SemiBold()
                .FontColor(Colors.Blue.Medium);
        }

        private void ComposePatientInfo(IContainer container, ProcedureDetailsViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Patient Name:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.PatientName).FontSize(10);
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("MR Number:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.PatientMRNumber).FontSize(10);
                        });
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Report Date:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(10);
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Report ID:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.ProcedureId.ToString().Substring(0, 8) + "...").FontSize(10);
                        });
                    });
                });
            });
        }

        private void ComposeProcedureInfo(IContainer container, ProcedureDetailsViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Procedure Date:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.ProcedureDate.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Region of Interest:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.ProcedureROI).FontSize(10);
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Duration:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.ProcedureDuration).FontSize(10);
                        });
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Status:").FontSize(10).SemiBold();
                            r.RelativeItem().Element(e =>
                            {
                                var statusBg = model.ProcedureStatus == "Completed" ? "#d4edda" :
                                              model.ProcedureStatus == "Scheduled" ? "#fff3cd" :
                                              model.ProcedureStatus == "In-Progress" ? "#cfe2ff" :
                                              "#f8d7da";

                                var statusColor = model.ProcedureStatus == "Completed" ? Colors.Green.Darken2 :
                                                 model.ProcedureStatus == "Scheduled" ? Colors.Orange.Darken2 :
                                                 model.ProcedureStatus == "In-Progress" ? Colors.Blue.Darken2 :
                                                 Colors.Red.Darken2;

                                e.Background(statusBg)
                                    .Padding(5)
                                    .Text(model.ProcedureStatus)
                                    .FontSize(9)
                                    .SemiBold()
                                    .FontColor(statusColor);
                            });
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Equipment Used:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.ProcedureEquipmentUsed ?? "Not specified").FontSize(10);
                        });
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text("Created by:").FontSize(10).SemiBold();
                            r.RelativeItem().Text(model.CreatedByName).FontSize(10);
                        });
                    });
                });
            });
        }

        private void ComposeMedicalRecord(IContainer container, ProcedureDetailsViewModel model)
        {
            container.Background("#f8f9fa").Padding(10).Column(column =>
            {
                // Cancer Types
                if (model.MedicalRecordCancerType?.Any() == true)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(140).Text("Cancer Types:").FontSize(10).SemiBold();
                        row.RelativeItem().Row(innerRow =>
                        {
                            foreach (var type in model.MedicalRecordCancerType)
                            {
                                innerRow.AutoItem().Padding(3).Background(Colors.Blue.Medium)
                                    .Padding(5)
                                    .Text(type)
                                    .FontSize(9)
                                    .FontColor(Colors.White);
                            }
                        });
                    });
                }

                // Two Column Layout
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(model.MedicalRecordCancerStage))
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(140).Text("Cancer Stage:").FontSize(10).SemiBold();
                                r.RelativeItem().Text(model.MedicalRecordCancerStage).FontSize(10);
                            });
                        }
                        if (!string.IsNullOrEmpty(model.MedicalRecordTumorLocation))
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(140).Text("Tumor Location:").FontSize(10).SemiBold();
                                r.RelativeItem().Text(model.MedicalRecordTumorLocation).FontSize(10);
                            });
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(model.MedicalRecordComorbidities))
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(140).Text("Comorbidities:").FontSize(10).SemiBold();
                                r.RelativeItem().Text(model.MedicalRecordComorbidities).FontSize(10);
                            });
                        }
                    });
                });

                // Allergies Warning
                if (!string.IsNullOrEmpty(model.MedicalRecordAllergies))
                {
                    column.Item().PaddingTop(8).Background("#fff3cd")
                        .Border(1)
                        .BorderColor(Colors.Orange.Medium)
                        .Padding(8)
                        .Row(row =>
                        {
                            row.AutoItem().PaddingRight(5).Text("⚠️").FontSize(14);
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Tumor Size").FontSize(10).SemiBold().FontColor(Colors.Red.Darken2);
                                col.Item().Text(model.MedicalRecordAllergies).FontSize(9).FontColor(Colors.Red.Darken1);
                            });
                        });
                }

                // Clinical History
                if (!string.IsNullOrEmpty(model.MedicalRecordClinicalHistory))
                {
                    column.Item().PaddingTop(8).Column(col =>
                    {
                        col.Item().Text("Clinical History:").FontSize(10).SemiBold();
                        col.Item().PaddingTop(3).Background(Colors.White)
                            .Padding(8)
                            .Text(model.MedicalRecordClinicalHistory)
                            .FontSize(9);
                    });
                }
            });
        }

        // ✅ FIXED: Wrapped in Column to avoid container reuse
        private void ComposeItemsTable(IContainer container, List<ProcedureItemSummaryViewModel> items)
        {
            container.Column(column =>
            {
                // Table
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Item Name").FontSize(9).SemiBold();
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Code").FontSize(9).SemiBold();
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Assigned").FontSize(9).SemiBold();
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Used").FontSize(9).SemiBold();
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Remaining").FontSize(9).SemiBold();
                        header.Cell().Background("#e9ecef").Padding(5)
                            .Text("Status").FontSize(9).SemiBold();
                    });

                    // Rows
                    foreach (var item in items)
                    {
                        var bgColor = items.IndexOf(item) % 2 == 0 ? "#ffffff" : "#f8f9fa";

                        table.Cell().Background(bgColor).Padding(5)
                            .Text(item.ItemName).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text(item.ItemCode).FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text($"{item.QuantityAssigned} {item.Unit}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text($"{item.QuantityUsed} {item.Unit}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text($"{item.RemainingQuantity} {item.Unit}").FontSize(9);
                        table.Cell().Background(bgColor).Padding(5)
                            .Text(item.Status).FontSize(9).SemiBold();
                    }
                });

                // Summary - Separate column.Item()
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text($"Total Assigned: {items.Sum(i => i.QuantityAssigned)}")
                        .FontSize(10).SemiBold();
                    row.RelativeItem().AlignCenter().Text($"Total Used: {items.Sum(i => i.QuantityUsed)}")
                        .FontSize(10).SemiBold();
                });
            });
        }

        private void ComposeClinicalReportsTable(IContainer container, List<ProcedureClinicalReportViewModel> reports)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background("#e9ecef").Padding(5)
                        .Text("Report Type").FontSize(9).SemiBold();
                    header.Cell().Background("#e9ecef").Padding(5)
                        .Text("Report Date").FontSize(9).SemiBold();
                    header.Cell().Background("#e9ecef").Padding(5)
                        .Text("Linked By").FontSize(9).SemiBold();
                    header.Cell().Background("#e9ecef").Padding(5)
                        .Text("Linked Date").FontSize(9).SemiBold();
                });

                // Rows
                foreach (var report in reports)
                {
                    var bgColor = reports.IndexOf(report) % 2 == 0 ? "#ffffff" : "#f8f9fa";

                    table.Cell().Background(bgColor).Padding(5)
                        .Text(report.ReportType).FontSize(9);
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(report.ReportDate.ToString("dd/MM/yyyy")).FontSize(9);
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(report.LinkedByName).FontSize(9);
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(report.LinkedAt.ToString("dd/MM/yyyy")).FontSize(9);
                }
            });
        }

        private byte[] ConvertHtmlToPdf(string htmlContent)
        {
            try
            {
                // Create HTML to PDF converter instance
                var converter = new HtmlToPdf();

                //converter.Options.UseHttpStreams = true;
                //converter.Options.NoExternalProcesses = true;

                // Set converter options
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.WebPageWidth = 1024;
                converter.Options.WebPageHeight = 0; // Auto height

                // Set margins (in points)
                converter.Options.MarginTop = 20;
                converter.Options.MarginBottom = 20;
                converter.Options.MarginLeft = 20;
                converter.Options.MarginRight = 20;

                // Additional rendering options
                converter.Options.JavaScriptEnabled = false;
                converter.Options.EmbedFonts = true;
                converter.Options.KeepImagesTogether = true;
                converter.Options.AutoFitWidth = HtmlToPdfPageFitMode.ShrinkOnly;

                // Enable CSS media type for print
                converter.Options.CssMediaType = HtmlToPdfCssMediaType.Print;

                // Convert HTML string to PDF document
                var pdfDocument = converter.ConvertHtmlString(htmlContent);

                // Save PDF to byte array
                var pdfBytes = pdfDocument.Save();

                // Close the PDF document
                pdfDocument.Close();

                return pdfBytes;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage2"] = ex.Message;
                // Log the error if you have a logger
                Console.WriteLine($"Error converting HTML to PDF: {ex.Message}");
                //_logger.LogError(ex, "Error converting HTML to PDF");
                return null;
            }

        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unknown";

            // Remove invalid characters
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            return fileName.Trim();
        }

        private string GenerateHtmlReport(ProcedureDetailsViewModel model)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Procedure Report - {model.PatientName}</title>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 0;
            padding: 20px;
            color: #333; 
            line-height: 1.6;
            font-size: 12px;
        }}
        
        .header {{ 
            text-align: center; 
            border-bottom: 3px solid #0d6efd; 
            padding-bottom: 20px; 
            margin-bottom: 30px;
        }}
        
        .logo {{ 
            font-size: 24px; 
            font-weight: bold; 
            color: #0d6efd; 
            margin-bottom: 10px;
        }}
        
        .report-title {{ 
            font-size: 20px; 
            margin: 10px 0;
            color: #333;
            font-weight: bold;
        }}
        
        .section {{ 
            margin: 25px 0; 
            page-break-inside: avoid;
        }}
        
        .section-title {{ 
            font-size: 14px; 
            font-weight: bold; 
            color: #0d6efd; 
            margin-bottom: 15px;
            padding: 8px 12px;
            background: #f8f9fa;
            border-left: 4px solid #0d6efd;
        }}
        
        .info-table {{ 
            width: 100%; 
            margin: 15px 0;
        }}
        
        .info-row {{ 
            margin: 8px 0;
        }}
        
        .info-label {{ 
            font-weight: bold; 
            color: #555;
            display: inline-block;
            min-width: 140px;
        }}
        
        .status {{ 
            padding: 4px 8px; 
            border-radius: 12px; 
            font-size: 10px; 
            font-weight: bold;
        }}
        
        .status-completed {{ 
            background: #d4edda; 
            color: #155724; 
        }}
        
        .status-scheduled {{ 
            background: #fff3cd; 
            color: #856404; 
        }}
        
        .status-inprogress {{ 
            background: #d1ecf1; 
            color: #0c5460; 
        }}
        
        .status-cancelled {{ 
            background: #f8d7da; 
            color: #721c24; 
        }}
        
        table {{ 
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
            font-size: 11px;
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
        
        tr:nth-child(even) {{
            background-color: #f9f9f9;
        }}
        
        .medical-record {{ 
            background: #f8f9fa; 
            padding: 15px; 
            border-radius: 6px; 
            margin: 10px 0;
        }}
        
        .cancer-type {{ 
            display: inline-block;
            background: #0d6efd; 
            color: white; 
            padding: 3px 8px; 
            border-radius: 10px; 
            font-size: 10px;
            margin: 2px 4px 2px 0;
        }}
        
        .alert {{ 
            padding: 12px; 
            border-radius: 4px; 
            margin: 10px 0;
        }}
        
        .alert-warning {{ 
            background: #fff3cd; 
            color: #856404;
            border-left: 4px solid #ffc107;
        }}
        
        .board-approval {{ 
            text-align: center; 
            padding: 20px; 
            margin: 20px 0;
            background: #f8f9fa;
            border-radius: 8px;
        }}
        
        .approval-status {{
            font-size: 18px; 
            font-weight: bold; 
            margin-bottom: 10px;
        }}
        
        .approved {{ 
            color: #198754; 
        }}
        
        .not-approved {{ 
            color: #dc3545; 
        }}
        
        .pending {{ 
            color: #ffc107; 
        }}
        
        .footer {{ 
            text-align: center; 
            margin-top: 40px; 
            padding-top: 20px; 
            border-top: 2px solid #dee2e6; 
            font-size: 10px; 
            color: #666;
        }}
        
        .two-column {{
            width: 100%;
        }}
        
        .two-column td {{
            width: 50%;
            vertical-align: top;
            padding-right: 15px;
        }}
    </style>
</head>
<body>
    <!-- Header -->
    <div class='header'>
        <div class='logo'>🏥 CryoablationSoftware</div>
        <div class='report-title'>MEDICAL PROCEDURE REPORT</div>
        <div>Generated on {DateTime.Now:dd/MM/yyyy HH:mm}</div>
    </div>

    <!-- Patient Information -->
    <div class='section'>
        <div class='section-title'>PATIENT INFORMATION</div>
        <table class='two-column'>
            <tr>
                <td>
                    <div class='info-row'>
                        <span class='info-label'>Patient Name:</span> {model.PatientName}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>MR Number:</span> {model.PatientMRNumber}
                    </div>
                </td>
                <td>
                    <div class='info-row'>
                        <span class='info-label'>Report Date:</span> {DateTime.Now:dd/MM/yyyy}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Report ID:</span> {model.ProcedureId.ToString().Substring(0, 8)}...
                    </div>
                </td>
            </tr>
        </table>
    </div>

    <!-- Procedure Information -->
    <div class='section'>
        <div class='section-title'>PROCEDURE INFORMATION</div>
        <table class='two-column'>
            <tr>
                <td>
                    <div class='info-row'>
                        <span class='info-label'>Procedure Date:</span> {model.ProcedureDate:dd/MM/yyyy HH:mm}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Region of Interest:</span> {model.ProcedureROI}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Duration:</span> {model.ProcedureDuration}
                    </div>
                </td>
                <td>
                    <div class='info-row'>
                        <span class='info-label'>Status:</span> 
                        <span class='status status-{model.ProcedureStatus.ToLower().Replace("-", "")}'>{model.ProcedureStatus}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Equipment Used:</span> {model.ProcedureEquipmentUsed ?? "Not specified"}
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Created by:</span> {model.CreatedByName}
                    </div>
                </td>
            </tr>
        </table>
        
        {(!string.IsNullOrEmpty(model.ProcedureRemarks) ? $@"
        <div class='info-row'>
            <span class='info-label'>Procedure Notes:</span><br>
            <div style='margin-top: 8px; padding: 10px; background: #f8f9fa; border-radius: 4px;'>
                {model.ProcedureRemarks}
            </div>
        </div>" : "")}
    </div>";

            // Medical Record Section
            if (model.HasMedicalRecord)
            {
                html += $@"
    <div class='section'>
        <div class='section-title'>MEDICAL RECORD</div>
        <div class='medical-record'>";

                if (model.MedicalRecordCancerType?.Any() == true)
                {
                    html += $@"
            <div class='info-row'>
                <span class='info-label'>Cancer Types:</span><br>
                {string.Join("", model.MedicalRecordCancerType.Select(ct => $"<span class='cancer-type'>{ct}</span>"))}
            </div>";
                }

                html += "<table class='two-column'><tr><td>";

                if (!string.IsNullOrEmpty(model.MedicalRecordCancerStage))
                {
                    html += $"<div class='info-row'><span class='info-label'>Cancer Stage:</span> {model.MedicalRecordCancerStage}</div>";
                }
                if (!string.IsNullOrEmpty(model.MedicalRecordTumorLocation))
                {
                    html += $"<div class='info-row'><span class='info-label'>Tumor Location:</span> {model.MedicalRecordTumorLocation}</div>";
                }

                html += "</td><td>";

                if (!string.IsNullOrEmpty(model.MedicalRecordComorbidities))
                {
                    html += $"<div class='info-row'><span class='info-label'>Comorbidities:</span> {model.MedicalRecordComorbidities}</div>";
                }

                html += "</td></tr></table>";

                if (!string.IsNullOrEmpty(model.MedicalRecordAllergies))
                {
                    html += $@"<div class='alert alert-warning'>
                <strong>⚠️ Tumor Size:</strong> {model.MedicalRecordAllergies}
            </div>";
                }

                if (!string.IsNullOrEmpty(model.MedicalRecordClinicalHistory))
                {
                    html += $@"<div class='info-row'>
                <span class='info-label'>Clinical History:</span><br>
                <div style='margin-top: 8px; padding: 10px; background: white; border-radius: 4px;'>
                    {model.MedicalRecordClinicalHistory}
                </div>
            </div>";
                }

                html += "</div></div>";
            }

            // Board Approval Section
            if (model.HasBoardApproval)
            {
                html += $@"
    <div class='section'>
        <div class='section-title'>BOARD APPROVAL</div>
        <div class='board-approval'>
            <div class='approval-status {(model.BoardApprovalDecision == true ? "approved" : model.BoardApprovalDecision == false ? "not-approved" : "pending")}'>
                {(model.BoardApprovalDecision == true ? "✅ APPROVED" : model.BoardApprovalDecision == false ? "❌ NOT APPROVED" : "⏳ PENDING")}
            </div>";

                if (model.BoardApprovalDate.HasValue || !string.IsNullOrEmpty(model.BoardApprovalLetterNumber))
                {
                    html += "<table class='two-column'><tr>";
                    if (model.BoardApprovalDate.HasValue)
                    {
                        html += $"<td><div class='info-row'><span class='info-label'>Approval Date:</span> {model.BoardApprovalDate.Value:dd/MM/yyyy}</div></td>";
                    }
                    if (!string.IsNullOrEmpty(model.BoardApprovalLetterNumber))
                    {
                        html += $"<td><div class='info-row'><span class='info-label'>Letter Number:</span> {model.BoardApprovalLetterNumber}</div></td>";
                    }
                    html += "</tr></table>";
                }

                html += "</div></div>";
            }

            // Items Section
            if (model.ItemSummaries?.Any() == true)
            {
                html += $@"
    <div class='section'>
        <div class='section-title'>ITEMS USED</div>
        <table>
            <thead>
                <tr>
                    <th>Item Name</th>
                    <th>Code</th>
                    <th>Assigned</th>
                    <th>Used</th>
                    <th>Remaining</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", model.ItemSummaries.Select(item => $@"
                <tr>
                    <td><strong>{item.ItemName}</strong></td>
                    <td>{item.ItemCode}</td>
                    <td>{item.QuantityAssigned} {item.Unit}</td>
                    <td>{item.QuantityUsed} {item.Unit}</td>
                    <td>{item.RemainingQuantity} {item.Unit}</td>
                    <td>{item.Status}</td>
                </tr>"))}
            </tbody>
        </table>
        
        <table class='two-column'>
            <tr>
                <td style='text-align: center;'>
                    <strong>Total Assigned: {model.ItemSummaries.Sum(i => i.QuantityAssigned)}</strong>
                </td>
                <td style='text-align: center;'>
                    <strong>Total Used: {model.ItemSummaries.Sum(i => i.QuantityUsed)}</strong>
                </td>
            </tr>
        </table>
    </div>";
            }

            // Clinical Reports Section
            if (model.ClinicalReports?.Any() == true)
            {
                html += $@"
    <div class='section'>
        <div class='section-title'>CLINICAL REPORTS</div>
        <table>
            <thead>
                <tr>
                    <th>Report Type</th>
                    <th>Report Date</th>
                    <th>Linked By</th>
                    <th>Linked Date</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", model.ClinicalReports.Select(report => $@"
                <tr>
                    <td><strong>{report.ReportType}</strong></td>
                    <td>{report.ReportDate:dd/MM/yyyy}</td>
                    <td>{report.LinkedByName}</td>
                    <td>{report.LinkedAt:dd/MM/yyyy}</td>
                </tr>"))}
            </tbody>
        </table>
    </div>";
            }

            html += $@"
    <div class='footer'>
        <div><strong>© {DateTime.Now.Year} CryoablationSoftware - Hospital Management System</strong></div>
        <div>This is a computer-generated report. No signature required.</div>
        <div style='margin-top: 10px;'>
            Report Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
        </div>
    </div>
</body>
</html>";

            return html;
        }


        #endregion



        #region By Patient - View Patient's All Procedures
        [HttpGet]
        public async Task<IActionResult> ByPatients(Guid patientId)
        {
            // Get patient details
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToAction("Procedures");
            }

            // Get all procedures for this patient
            var procedures = await _context.Procedures
                .Where(p => p.PatientId == patientId && p.IsActive)
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                    .ThenInclude(ia => ia.Item)
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .OrderByDescending(p => p.ProcedureDate)
                .Select(p => new ProcedureListViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientName = patient.PatientName,
                    PatientMRNumber = patient.PatientMRNumber,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    CreatedByName = p.CreatedBy.UserName ?? "Unknown",
                    CreatedAt = p.CreatedAt,
                    ItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    ItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed)
                })
                .ToListAsync();

            var model = new PatientProceduresViewModel
            {
                PatientId = patientId,
                PatientName = patient.PatientName,
                PatientMRNumber = patient.PatientMRNumber,
                PatientPhone = patient.PatientPhone,
                PatientAge = patient.PatientAge,
                PatientGender = patient.PatientGender,
                Procedures = procedures
            };

            return View(model);
        }
        #endregion

        #region AJAX Methods

        [HttpGet]
        public async Task<JsonResult> GetItemStock(Guid itemId)
        {
            var stock = await _procedureService.GetAvailableStockAsync(itemId);
            return Json(new { stock = stock });
        }

        [HttpGet]
        public async Task<JsonResult> GetPatientDetails(Guid patientId)
        {
            try
            {
                var patients = await _procedureService.GetActivePatientsAsync();
                var patient = patients.FirstOrDefault(p => p.PatientId == patientId);

                if (patient != null)
                {
                    return Json(new
                    {
                        patientName = patient.PatientName,
                        mrNumber = patient.PatientMRNumber,
                        phone = patient.PatientPhone,
                        age = patient.PatientAge,
                        gender = patient.PatientGender
                    });
                }

                return Json(new { error = "Patient not found" });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error loading patient details" });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetItemAssignmentInfo(Guid procedureId, Guid itemId)
        {
            var itemSummaries = await _procedureService.GetProcedureItemSummaryAsync(procedureId);
            // FIXED: Changed 'is' to 'item' in lambda expression
            var itemSummary = itemSummaries.FirstOrDefault(item => item.ItemId == itemId);

            if (itemSummary != null)
            {
                return Json(new
                {
                    itemName = itemSummary.ItemName,
                    itemCode = itemSummary.ItemCode,
                    unit = itemSummary.Unit,
                    quantityAssigned = itemSummary.QuantityAssigned,
                    quantityUsed = itemSummary.QuantityUsed,
                    remainingQuantity = itemSummary.RemainingQuantity
                });
            }

            return Json(new { error = "Item not found" });
        }

        #endregion

        #region Private Helper Methods

        private async Task LoadCreateViewData()
        {
            try
            {
                var patients = await _procedureService.GetActivePatientsAsync();
                var items = await _procedureService.GetAvailableItemsAsync();

                ViewBag.Patients = patients.Select(p => new {
                    Value = p.PatientId,
                    Text = $"{p.PatientName} ({p.PatientMRNumber})"
                }).ToList();

                ViewBag.Items = items.Select(i => new {
                    Value = i.ItemId,
                    Text = $"{i.ItemName} ({i.ItemCode})",
                    Unit = i.Unit
                }).ToList();

                ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
            }
            catch (Exception ex)
            {
                // Log error and provide empty lists to prevent view errors
                ViewBag.Patients = new List<dynamic>();
                ViewBag.Items = new List<dynamic>();
                ViewBag.ProcedureStatuses = new List<string> { "Scheduled" };

                TempData["ErrorMessage"] = "Error loading form data. Please try again.";
            }
        }

        //private async Task LoadEditViewData()
        //{
        //    var patients = await _procedureService.GetActivePatientsAsync();

        //    ViewBag.Patients = patients.Select(p => new {
        //        Value = p.PatientId,
        //        Text = $"{p.PatientName} ({p.PatientMRNumber})"
        //    }).ToList();

        //    ViewBag.ProcedureStatuses = ProcedureStatus.GetAllStatuses();
        //}

        private async Task LoadEditViewData()
        {
            ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
        }

        #endregion




        #region Status Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(Guid procedureId, string newStatus)
        {
            try
            {
                // If completing procedure, redirect to completion form
                if (newStatus == "Completed")
                {
                    return RedirectToAction(nameof(CompleteProcedure), new { id = procedureId });
                }

                var procedure = await _procedureService.GetProcedureByIdAsync(procedureId);
                if (procedure == null)
                {
                    TempData["ErrorMessage"] = "Procedure not found";
                    return RedirectToAction(nameof(Procedures));
                }

                var editModel = new ProcedureEditViewModel
                {
                    ProcedureId = procedure.ProcedureId,
                    PatientId = procedure.PatientId,
                    ProcedureDate = procedure.ProcedureDate,
                    ProcedureROI = procedure.ProcedureROI,
                    ProcedureDuration = procedure.ProcedureDuration,
                    ProcedureEquipmentUsed = procedure.ProcedureEquipmentUsed,
                    ProcedureRemarks = procedure.ProcedureRemarks,
                    ProcedureStatus = newStatus, // Update status
                    ProcedureNextDate = procedure.ProcedureNextDate,
                    PreviousProcedureId = procedure.PreviousProcedureId,
                    IsActive = procedure.IsActive
                };

                var result = await _procedureService.UpdateProcedureAsync(editModel);

                if (result)
                {
                    TempData["SuccessMessage"] = $"Procedure status changed to {newStatus} successfully";
                }
                else
                {
                    // Log status change failure
                    await _auditLogService.LogAsync(
                        actionType: "STATUS_CHANGE_FAILED",
                        message: "Failed to change procedure status",
                        recordId: procedureId.ToString(),
                        tableName: "Procedures",
                        additionalData: new { ProcedureId = procedureId, NewStatus = newStatus },
                        serviceName: "ProcedureController",
                        methodName: "ChangeStatus"
                    );

                    TempData["ErrorMessage"] = "Error changing procedure status";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error changing procedure status";
            }

            return RedirectToAction(nameof(Details), new { id = procedureId });
        }

        [HttpGet]
        public async Task<IActionResult> CompleteProcedure(Guid id)
        {
            var procedure = await _procedureService.GetProcedureByIdAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            var model = new ProcedureCompletionViewModel
            {
                ProcedureId = id,
                PatientName = procedure.Patient?.PatientName,
                ProcedureROI = procedure.ProcedureROI,
                ProcedureDate = procedure.ProcedureDate,
                DischargeDate = DateTime.Now.Date,
                NextProcedureDate = procedure.ProcedureNextDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProcedure(ProcedureCompletionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var procedure = await _procedureService.GetProcedureByIdAsync(model.ProcedureId);
                if (procedure == null)
                {
                    return NotFound();
                }

                // Update procedure with completion details
                var editModel = new ProcedureEditViewModel
                {
                    ProcedureId = procedure.ProcedureId,
                    PatientId = procedure.PatientId,
                    ProcedureDate = procedure.ProcedureDate,
                    ProcedureROI = procedure.ProcedureROI,
                    ProcedureDuration = procedure.ProcedureDuration,
                    ProcedureEquipmentUsed = procedure.ProcedureEquipmentUsed,
                    ProcedureRemarks = $"{procedure.ProcedureRemarks}\n\n--- COMPLETION DETAILS ---\nIR Consultant Remarks: {model.IRConsultantRemarks}\nDischarge Date: {model.DischargeDate?.ToString("dd/MM/yyyy")}\nCompletion Notes: {model.CompletionNotes}",
                    ProcedureStatus = "Completed",
                    ProcedureNextDate = model.NextProcedureDate,
                    PreviousProcedureId = procedure.PreviousProcedureId,
                    IsActive = procedure.IsActive
                };

                var result = await _procedureService.UpdateProcedureAsync(editModel);

                if (result)
                {
                    TempData["SuccessMessage"] = "Procedure completed successfully";
                    return RedirectToAction(nameof(PrintSummary), new { id = model.ProcedureId });
                }
                else
                {
                    // Log completion failure
                    await _auditLogService.LogAsync(
                        actionType: "COMPLETE_FAILED",
                        message: "Failed to complete procedure",
                        recordId: model.ProcedureId.ToString(),
                        tableName: "Procedures",
                        additionalData: new { ProcedureId = model.ProcedureId },
                        serviceName: "ProcedureController",
                        methodName: "CompleteProcedure"
                    );

                    TempData["ErrorMessage"] = "Error completing procedure";
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PrintSummary(Guid id)
        {
            var procedure = await _procedureService.GetProcedureDetailsAsync(id);
            if (procedure == null)
            {
                return NotFound();
            }

            return View(procedure);
        }

        #endregion


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid procedureId, string newStatus)
        {
            try
            {
                // Validate status
                var validStatuses = new[] { "Scheduled", "In-Progress", "Completed", "Cancelled" };
                if (!validStatuses.Contains(newStatus))
                {
                    return Json(new { success = false, message = "Invalid status provided" });
                }

                var result = await _procedureService.UpdateProcedureStatusAsync(procedureId, newStatus);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Procedure status updated to {newStatus} successfully"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Procedure not found or error updating status" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating procedure status" });
            }
        }

    }
}
