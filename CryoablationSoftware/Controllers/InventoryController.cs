using ClosedXML.Excel;
using CryoablationSoftware.Data;
using CryoablationSoftware.Services;
using CryoablationSoftware.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Security.Claims;

namespace CryoablationSoftware.Controllers
{
   // [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public InventoryController(IInventoryService inventoryService, UserManager<IdentityUser> userManager,   ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _inventoryService = inventoryService;
            _userManager = userManager;
            _context = context;
            _auditLogService = auditLogService;
        }

        #region Grouped Items Management

        public async Task<IActionResult> ItemsGrouped()
        {
            var groupedItems = await _inventoryService.GetGroupedItemsAsync();
            return View(groupedItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupedItemStock(string itemName)
        {
            try
            {
                var totalStock = await _inventoryService.GetGroupedItemStockAsync(itemName);
                return Json(new { success = true, stock = totalStock });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    actionType: "GET_GROUPED_STOCK_FAILED",
                    message: $"Failed to get grouped stock for {itemName}",
                    additionalData: new { ItemName = itemName, Error = ex.Message },
                    serviceName: "InventoryController",
                    methodName: "GetGroupedItemStock"
                );

                return Json(new { success = false, message = "Failed to load stock" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemVariantsDetail(string itemName)
        {
            try
            {
                var variants = await _inventoryService.GetItemVariantsByNameAsync(itemName);

                var variantsList = variants.Select(v => new
                {
                    itemId = v.ItemId,
                    itemCode = v.ItemCode,
                    itemName = v.ItemName,
                    category = v.Category,
                    unit = v.Unit,
                    currentStock = v.CurrentStock,
                    reorderLevel = v.ReorderLevel,
                    isActive = v.IsActive,
                    isLowStock = v.IsLowStock,
                    isOutOfStock = v.IsOutOfStock,
                    createdBy = v.CreatedByUser,
                    createdAt = v.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Json(new { success = true, variants = variantsList });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    actionType: "GET_VARIANTS_FAILED",
                    message: $"Failed to get variants for {itemName}",
                    additionalData: new { ItemName = itemName, Error = ex.Message },
                    serviceName: "InventoryController",
                    methodName: "GetItemVariantsDetail"
                );

                return Json(new { success = false, message = "Failed to load variants" });
            }
        }

        #endregion


        #region Dashboard

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _inventoryService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        #endregion

        #region Items Management

        public async Task<IActionResult> Items()
        {
            var items = await _inventoryService.GetAllItemsAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> CreateItem()
        {
            await LoadItemDropdowns();
            return View(new ItemCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(ItemCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if item code is unique
                var isUnique = await _inventoryService.IsItemCodeUniqueAsync(model.ItemCode);
                if (!isUnique)
                {
                    ModelState.AddModelError("ItemCode", "Item Code already exists");
                    await LoadItemDropdowns();
                    return View(model);
                }

                var userId = _userManager.GetUserId(User);
                var result = await _inventoryService.CreateItemAsync(model, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Item created successfully";
                    return RedirectToAction(nameof(Items));
                }
                else
                {
                    // Log creation failure
                    await _auditLogService.LogAsync(
                        actionType: "CREATE_ITEM_FAILED",
                        message: "Item creation failed",
                        tableName: "Items",
                        additionalData: new { ItemCode = model.ItemCode, ItemName = model.ItemName },
                        serviceName: "InventoryController",
                        methodName: "Create"
                    );

                    TempData["ErrorMessage"] = "Error creating item";
                }
            }

            await LoadItemDropdowns();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditItem(Guid id)
        {
            var item = await _inventoryService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var model = new ItemEditViewModel
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                ItemCode = item.ItemCode,
                Category = item.Category,
                ItemType = item.ItemType,
                Unit = item.Unit,
                ReorderLevel = item.ReorderLevel,
                Description = item.Description,
                IsActive = item.IsActive
            };

            await LoadItemDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(ItemEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if item code is unique (excluding current item)
                var isUnique = await _inventoryService.IsItemCodeUniqueAsync(model.ItemCode, model.ItemId);
                if (!isUnique)
                {
                    ModelState.AddModelError("ItemCode", "Item Code already exists");
                    await LoadItemDropdowns();
                    return View(model);
                }

                var result = await _inventoryService.UpdateItemAsync(model);

                if (result)
                {
                    TempData["SuccessMessage"] = "Item updated successfully";
                    return RedirectToAction(nameof(Items));
                }
                else
                {
                    // Log update failure
                    await _auditLogService.LogAsync(
                        actionType: "UPDATE_ITEM_FAILED",
                        message: "Item update failed",
                        recordId: model.ItemId.ToString(),
                        tableName: "Items",
                        additionalData: new { ItemId = model.ItemId, ItemName = model.ItemName },
                        serviceName: "InventoryController",
                        methodName: "Edit"
                    );

                    TempData["ErrorMessage"] = "Error updating item";
                }
            }

            await LoadItemDropdowns();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ItemDetails(Guid id)
        {
            var item = await _inventoryService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            ViewBag.CurrentStock = await _inventoryService.GetCurrentStockAsync(id);
            ViewBag.StockHistory = await _inventoryService.GetStockHistoryAsync(id);
            ViewBag.UsageHistory = await _inventoryService.GetUsageHistoryAsync(id);

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var result = await _inventoryService.DeleteItemAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Item deleted successfully";
            }
            else
            {
                // Log deletion failure
                await _auditLogService.LogAsync(
                    actionType: "DELETE_ITEM_FAILED",
                    message: "Item deletion failed",
                    recordId: id.ToString(),
                    tableName: "Items",
                    additionalData: new { ItemId = id },
                    serviceName: "InventoryController",
                    methodName: "Delete"
                );

                TempData["ErrorMessage"] = "Error deleting item";
            }

            return RedirectToAction(nameof(Items));
        }

        #endregion

        #region Stock Management

        [HttpGet]
        public async Task<IActionResult> ReceiveStock()
        {
            await LoadStockDropdowns();
            return View(new StockReceiveViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveStock(StockReceiveViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var result = await _inventoryService.ReceiveStockAsync(model, userId);

                if (result)
                {
                    TempData["SuccessMessage"] = "Stock received successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Log receive stock failure
                    await _auditLogService.LogAsync(
                        actionType: "RECEIVE_STOCK_FAILED",
                        message: "Stock receive operation failed",
                        tableName: "StockRecords",
                        additionalData: new { ItemId = model.ItemId, Quantity = model.QuantityReceived },
                        serviceName: "InventoryController",
                        methodName: "ReceiveStock"
                    );

                    TempData["ErrorMessage"] = "Error receiving stock";
                }
            }

            await LoadStockDropdowns();
            return View(model);
        }

        // Current Stock Details Page
        public async Task<IActionResult> CurrentStockDetails()
        {
            var items = await _context.Items
                .Where(i => i.IsActive)
                .Include(i => i.StockRecords.Where(sr => sr.IsActive))
                .Include(i => i.UsageRecords.Where(ur => ur.IsActive))
                .Include(i => i.ProcedureUsages.Where(pu => pu.IsActive))
                .ToListAsync();

            var stockDetails = items.Select(item => new
            {
                ItemName = item.ItemName,
                ItemCode = item.ItemCode,
                Category = item.Category,
                Unit = item.Unit,
                TotalReceived = item.StockRecords.Sum(sr => sr.QuantityReceived),
                TotalUsed = item.UsageRecords.Sum(ur => ur.QuantityUsed) +
                            item.ProcedureUsages.Sum(pu => pu.QuantityUsed),
                CurrentStock = item.StockRecords.Sum(sr => sr.QuantityReceived) -
                              (item.UsageRecords.Sum(ur => ur.QuantityUsed) +
                               item.ProcedureUsages.Sum(pu => pu.QuantityUsed))
            })
            .OrderByDescending(x => x.CurrentStock)
            .ToList();

            return View(stockDetails);
        }

        // Used Stock Details Page
        public async Task<IActionResult> UsedStockDetails()
        {
            var items = await _context.Items
                .Where(i => i.IsActive && i.ItemType == "Probe")
                .Include(i => i.UsageRecords.Where(ur => ur.IsActive && ur.Item.ItemType == "Probe" && ur.UsageReason != "Procedure"))
                .Include(i => i.ProcedureUsages.Where(pu => pu.IsActive && pu.Item.ItemType == "Probe"))
                .ToListAsync();

            var usedDetails = items.Select(item => new
            {
                ItemName = item.ItemName,
                ItemCode = item.ItemCode,
                Category = item.Category,
                Unit = item.Unit,
                UsedInProcedures = item.ProcedureUsages.Sum(pu => pu.QuantityUsed),
                UsedOther = item.UsageRecords.Sum(ur => ur.QuantityUsed ),
                TotalUsed = item.UsageRecords.Sum(ur => ur.QuantityUsed) +
                            item.ProcedureUsages.Sum(pu => pu.QuantityUsed)
            })
            .Where(x => x.TotalUsed > 0)
            .OrderByDescending(x => x.TotalUsed)
            .ToList();

            return View(usedDetails);
        }

        [HttpGet]
        public async Task<IActionResult> IssueStock()
        {
            await LoadIssueDropdowns();
            return View(new StockIssueViewModel());
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> IssueStock(StockIssueViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var userId = _userManager.GetUserId(User);
        //        var result = await _inventoryService.IssueStockAsync(model, userId);

        //        if (result)
        //        {
        //            TempData["SuccessMessage"] = "Stock issued successfully";
        //            return RedirectToAction(nameof(Index));
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Insufficient stock or error issuing stock";
        //        }
        //    }

        //    await LoadIssueDropdowns();
        //    return View(model);
        //}



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueStock(StockIssueViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Additional validation - Check current stock before processing
                    var currentStock = await _inventoryService.GetCurrentStockAsync(model.ItemId);
                    if (currentStock < model.QuantityUsed)
                    {
                        ModelState.AddModelError("QuantityUsed", $"Insufficient stock. Only {currentStock} units available.");
                        await LoadIssueDropdowns();
                        return View(model);
                    }

                    // Get item details for logging/display
                    var item = await _inventoryService.GetItemByIdAsync(model.ItemId);
                    if (item == null)
                    {
                        ModelState.AddModelError("ItemId", "Selected item not found.");
                        await LoadIssueDropdowns();
                        return View(model);
                    }

                    // Set ItemName for display purposes
                    model.ItemName = item.ItemName;

                    var userId = _userManager.GetUserId(User);
                    var result = await _inventoryService.IssueStockAsync(model, userId);

                    if (result)
                    {
                        TempData["SuccessMessage"] = $"Successfully issued {model.QuantityUsed} {item.Unit} of {item.ItemName}";

                        // Redirect based on context
                        if (model.PatientId.HasValue)
                        {
                            // If linked to patient, could redirect to patient details
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else
                    {
                        // Log issue stock failure
                        await _auditLogService.LogAsync(
                            actionType: "ISSUE_STOCK_FAILED",
                            message: "Stock issue operation failed",
                            tableName: "UsageRecords",
                            additionalData: new { ItemId = model.ItemId, Quantity = model.QuantityUsed },
                            serviceName: "InventoryController",
                            methodName: "IssueStock"
                        );

                        TempData["ErrorMessage"] = "Error occurred while issuing stock. Please try again.";
                        ModelState.AddModelError("", "Failed to issue stock. This might be due to insufficient stock or system error.");
                    }
                }
                catch (Exception ex)
                {
                    // Log error if you have logging configured
                    // _logger.LogError(ex, "Error in IssueStock action for ItemId: {ItemId}", model.ItemId);

                    // Log unexpected error
                    await _auditLogService.LogAsync(
                        actionType: "ISSUE_STOCK_ERROR",
                        message: "Unexpected error during stock issue",
                        tableName: "UsageRecords",
                        additionalData: new { ItemId = model.ItemId, Quantity = model.QuantityUsed },
                        serviceName: "InventoryController",
                        methodName: "IssueStock"
                    );

                    TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                    ModelState.AddModelError("", "An unexpected error occurred while processing your request.");
                }
            }
            else
            {
                // Log validation errors for debugging
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Stock issue validation failed",
                    tableName: "UsageRecords",
                    additionalData: new { ItemId = model.ItemId, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() },
                    serviceName: "InventoryController",
                    methodName: "IssueStock"
                );

                TempData["ErrorMessage"] = "Please correct the form errors and try again.";
            }

            await LoadIssueDropdowns();
            return View(model);
        }


        [HttpGet]
        public async Task<JsonResult> GetCurrentStock(Guid itemId)
        {
            var currentStock = await _inventoryService.GetCurrentStockAsync(itemId);
            return Json(new { stock = currentStock });
        }

        [HttpGet]
        public async Task<JsonResult> GetItemDetails(Guid itemId)
        {
            var item = await _inventoryService.GetItemByIdAsync(itemId);
            var currentStock = await _inventoryService.GetCurrentStockAsync(itemId);

            return Json(new
            {
                itemName = item?.ItemName,
                unit = item?.Unit,
                currentStock = currentStock
            });
        }


        [HttpGet]
        public async Task<JsonResult> GetPatients()
        {
            try
            {
                // Adjust this based on your Patient service/repository
                var patients = await _context.Patients
                    .Where(p => p.IsActive)
                    .Select(p => new {
                        patientId = p.PatientId,
                        patientName = p.PatientName,
                        mrNumber = p.PatientMRNumber
                    })
                    .OrderBy(p => p.patientName)
                    .Take(50) // Limit for performance
                    .ToListAsync();

                return Json(patients);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        #endregion

        #region Suppliers Management

        //public async Task<IActionResult> Suppliers()
        //{
        //    var suppliers = await _inventoryService.GetAllSuppliersAsync();
        //    return View(suppliers);
        //}

        //[HttpGet]
        //public IActionResult CreateSupplier()
        //{
        //    return View(new SupplierCreateViewModel());
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateSupplier(SupplierCreateViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var result = await _inventoryService.CreateSupplierAsync(model);

        //        if (result)
        //        {
        //            TempData["SuccessMessage"] = "Supplier created successfully";
        //            return RedirectToAction(nameof(Suppliers));
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Error creating supplier";
        //        }
        //    }

        //    return View(model);
        //}

        //[HttpGet]
        //public async Task<IActionResult> SupplierDetails(Guid id)
        //{
        //    var supplier = await _inventoryService.GetSupplierByIdAsync(id);
        //    if (supplier == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(supplier);
        //}

        #endregion

        #region Reports

        public async Task<IActionResult> StockReport()
        {
            var reportData = await _inventoryService.GetStockReportAsync();
            return View(reportData);
        }

        public async Task<IActionResult> LowStockReport()
        {
            var lowStockItems = await _inventoryService.GetLowStockItemsAsync();
            return View(lowStockItems);
        }

        #endregion

        #region Private Helper Methods

        private async Task LoadItemDropdowns()
        {
            var categories = await _inventoryService.GetCategoriesAsync();
            var units = await _inventoryService.GetUnitsAsync();

            ViewBag.Categories = categories.Any()
                ? categories.ToList()
                : new List<string> { "Consumables", "Equipment", "Medicines", "Supplies" };

            ViewBag.Units = units.Any()
                ? units.ToList()
                : new List<string> { "Pieces", "Box", "Pack", "Liters", "KG", "Meters" };
        }

        private async Task LoadStockDropdowns()
        {
            var items = await _inventoryService.GetAllItemsAsync();
            // var suppliers = await _inventoryService.GetAllSuppliersAsync();

            ViewBag.Items = items.Select(i => new {
                Value = i.ItemId,
                Text = $"{i.ItemName} ({i.ItemCode})"
            }).ToList();

            //ViewBag.Suppliers = suppliers.Select(s => new {
            //    Value = s.SupplierId,
            //    Text = s.SupplierName
            //}).ToList();
        }

        private async Task LoadIssueDropdowns()
        {
            var items = await _inventoryService.GetAllItemsAsync();

            ViewBag.Items = items.Select(i => new {
                Value = i.ItemId,
                Text = $"{i.ItemName} ({i.ItemCode})"
            }).ToList();

            ViewBag.UsageReasons = new List<string>
            {
                "Procedure",
                "Maintenance",
                "Damaged",
                "Expired",
                "Training",
                "Other"
            };

            // Load patients for dropdown (optional - if needed)
            // You can implement this based on your Patient service
        }

        #endregion


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleItemStatus(Guid itemId)
        {
            try
            {
                var item = await _inventoryService.GetItemByIdAsync2(itemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                // Create updated model with toggled status
                var model = new ItemEditViewModel
                {
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    ItemCode = item.ItemCode,
                    Category = item.Category,
                    Unit = item.Unit,
                    ReorderLevel = item.ReorderLevel,
                    Description = item.Description,
                    IsActive = !item.IsActive // Toggle the status
                };

                var result = await _inventoryService.UpdateItemAsync(model);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Item status updated to {(model.IsActive ? "Active" : "Inactive")} successfully",
                        newStatus = model.IsActive
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Error updating item status" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating item status" });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetCurrentStock2(Guid itemId)
        {
            try
            {
                var item = await _inventoryService.GetItemByIdAsync(itemId);
                if (item == null)
                {
                    return Json(new { stock = 0, error = "Item not found" });
                }

                var currentStock = await _inventoryService.GetCurrentStockAsync(itemId);

                return Json(new
                {
                    stock = currentStock,
                    unit = item.Unit,
                    reorderLevel = item.ReorderLevel,
                    isLowStock = currentStock <= item.ReorderLevel,
                    itemName = item.ItemName
                });
            }
            catch (Exception ex)
            {
                return Json(new { stock = 0, error = "Error retrieving stock information" });
            }
        }

        [HttpGet]
        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Excel upload validation failed - no file selected",
                    tableName: "Items",
                    serviceName: "InventoryController",
                    methodName: "UploadExcel"
                );

                TempData["ErrorMessage"] = "Please select a valid Excel file";
                return View();
            }

            // Check file extension
            var fileExtension = Path.GetExtension(excelFile.FileName).ToLower();
            if (fileExtension != ".xlsx" && fileExtension != ".xls")
            {
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Excel upload validation failed - invalid file type",
                    tableName: "Items",
                    additionalData: new { FileName = excelFile.FileName },
                    serviceName: "InventoryController",
                    methodName: "UploadExcel"
                );

                TempData["ErrorMessage"] = "Please upload only Excel files (.xlsx or .xls)";
                return View();
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _inventoryService.UploadExcelDataAsync(excelFile, userId);

                return View("UploadExcelResult", result);
            }
            catch (Exception ex)
            {
                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "UploadExcel",
                    customMessage: "Error processing Excel file",
                    additionalData: new { FileName = excelFile?.FileName },
                    serviceName: "InventoryController"
                );

                TempData["ErrorMessage"] = $"Error processing file: {ex.Message}";
                return View();
            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportItemsToExcel()
        {
            try
            {
                var items = await _inventoryService.GetAllItemsAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Items Inventory Report");

                    // Headers
                    worksheet.Cell(1, 1).Value = "Item Code";
                    worksheet.Cell(1, 2).Value = "Item Name";
                    worksheet.Cell(1, 3).Value = "Category";
                    worksheet.Cell(1, 4).Value = "Unit";
                    worksheet.Cell(1, 5).Value = "Current Stock";
                    worksheet.Cell(1, 6).Value = "Reorder Level";
                    worksheet.Cell(1, 7).Value = "Stock Status";
                    worksheet.Cell(1, 8).Value = "Item Status";
                    worksheet.Cell(1, 9).Value = "Created By";
                    worksheet.Cell(1, 10).Value = "Created Date";
                    worksheet.Cell(1, 11).Value = "Description";

                    // Style headers
                    var headerRange = worksheet.Range(1, 1, 1, 11);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Data
                    int row = 2;
                    foreach (var item in items)
                    {
                        var currentStock = await _inventoryService.GetCurrentStockAsync(item.ItemId);

                        worksheet.Cell(row, 1).Value = item.ItemCode;
                        worksheet.Cell(row, 2).Value = item.ItemName;
                        worksheet.Cell(row, 3).Value = item.Category;
                        worksheet.Cell(row, 4).Value = item.Unit;
                        worksheet.Cell(row, 5).Value = currentStock;
                        worksheet.Cell(row, 6).Value = item.ReorderLevel;

                        // Stock Status with color coding
                        string stockStatus;
                        XLColor stockColor;
                        XLColor bgColor = XLColor.White;

                        if (currentStock <= 0)
                        {
                            stockStatus = "Out of Stock";
                            stockColor = XLColor.Red;
                            bgColor = XLColor.FromArgb(255, 230, 230);
                        }
                        else if (currentStock <= item.ReorderLevel)
                        {
                            stockStatus = "Low Stock";
                            stockColor = XLColor.Orange;
                            bgColor = XLColor.FromArgb(255, 248, 220);
                        }
                        else
                        {
                            stockStatus = "In Stock";
                            stockColor = XLColor.Green;
                        }

                        worksheet.Cell(row, 7).Value = stockStatus;
                        worksheet.Cell(row, 7).Style.Font.FontColor = stockColor;
                        worksheet.Cell(row, 7).Style.Font.Bold = true;

                        worksheet.Cell(row, 8).Value = item.IsActive ? "Active" : "Inactive";
                        worksheet.Cell(row, 9).Value = item.CreatedBy?.UserName ?? "System";
                        worksheet.Cell(row, 10).Value = item.CreatedAt.ToString("dd/MM/yyyy");
                        worksheet.Cell(row, 11).Value = item.Description ?? "";

                        // Highlight critical stock levels
                        if (currentStock <= item.ReorderLevel)
                        {
                            var rowRange = worksheet.Range(row, 1, row, 11);
                            rowRange.Style.Fill.BackgroundColor = bgColor;
                        }

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.ColumnsUsed().AdjustToContents();

                    // Add summary section
                    var summaryStartRow = row + 2;
                    worksheet.Cell(summaryStartRow, 1).Value = "INVENTORY SUMMARY";
                    worksheet.Cell(summaryStartRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(summaryStartRow, 1).Style.Font.FontSize = 14;

                    // Summary data
                    var totalItems = items.Count();
                    var activeItems = items.Count(i => i.IsActive);
                    var inactiveItems = items.Count(i => !i.IsActive);
                    var totalCategories = items.Select(i => i.Category).Distinct().Count();

                    // Calculate stock summary
                    var outOfStockCount = 0;
                    var lowStockCount = 0;
                    var inStockCount = 0;

                    foreach (var item in items)
                    {
                        var stock = await _inventoryService.GetCurrentStockAsync(item.ItemId);
                        if (stock <= 0) outOfStockCount++;
                        else if (stock <= item.ReorderLevel) lowStockCount++;
                        else inStockCount++;
                    }

                    worksheet.Cell(summaryStartRow + 2, 1).Value = "Total Items:";
                    worksheet.Cell(summaryStartRow + 2, 2).Value = totalItems;

                    worksheet.Cell(summaryStartRow + 3, 1).Value = "Active Items:";
                    worksheet.Cell(summaryStartRow + 3, 2).Value = activeItems;

                    worksheet.Cell(summaryStartRow + 4, 1).Value = "Inactive Items:";
                    worksheet.Cell(summaryStartRow + 4, 2).Value = inactiveItems;

                    worksheet.Cell(summaryStartRow + 5, 1).Value = "Categories:";
                    worksheet.Cell(summaryStartRow + 5, 2).Value = totalCategories;

                    worksheet.Cell(summaryStartRow + 7, 1).Value = "STOCK STATUS SUMMARY";
                    worksheet.Cell(summaryStartRow + 7, 1).Style.Font.Bold = true;

                    worksheet.Cell(summaryStartRow + 8, 1).Value = "In Stock:";
                    worksheet.Cell(summaryStartRow + 8, 2).Value = inStockCount;
                    worksheet.Cell(summaryStartRow + 8, 2).Style.Font.FontColor = XLColor.Green;

                    worksheet.Cell(summaryStartRow + 9, 1).Value = "Low Stock:";
                    worksheet.Cell(summaryStartRow + 9, 2).Value = lowStockCount;
                    worksheet.Cell(summaryStartRow + 9, 2).Style.Font.FontColor = XLColor.Orange;

                    worksheet.Cell(summaryStartRow + 10, 1).Value = "Out of Stock:";
                    worksheet.Cell(summaryStartRow + 10, 2).Value = outOfStockCount;
                    worksheet.Cell(summaryStartRow + 10, 2).Style.Font.FontColor = XLColor.Red;

                    worksheet.Cell(summaryStartRow + 12, 1).Value = "Generated On:";
                    worksheet.Cell(summaryStartRow + 12, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    worksheet.Cell(summaryStartRow + 13, 1).Value = "Generated By:";
                    worksheet.Cell(summaryStartRow + 13, 2).Value = User.Identity?.Name ?? "System";

                    // Style summary section
                    var summaryRange = worksheet.Range(summaryStartRow, 1, summaryStartRow + 13, 2);
                    summaryRange.Style.Font.Bold = true;
                    summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var fileName = $"Items_Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileContents = stream.ToArray();
                        return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log export failure
                await _auditLogService.LogAsync(
                    actionType: "EXPORT_FAILED",
                    message: "Excel export operation failed",
                    tableName: "Items",
                    serviceName: "InventoryController",
                    methodName: "ExportToExcel"
                );

                TempData["ErrorMessage"] = "Error occurred while exporting data. Please try again.";
                return RedirectToAction("Items");
            }
        }

    }
}
