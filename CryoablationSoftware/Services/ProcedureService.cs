using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CryoablationSoftware.Services
{
    public class ProcedureService : IProcedureService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditLogService _auditLogService;


        public ProcedureService(ApplicationDbContext context, IInventoryService inventoryService, IAuditLogService auditLogService)
        {
            _context = context;
            _inventoryService = inventoryService;
            _auditLogService = auditLogService;
        }

        #region Procedure Management

        public async Task<IEnumerable<ProcedureListViewModel>> GetAllProceduresAsync()
        {
            return await _context.Procedures
                .Where(p => p.IsActive)
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .Select(p => new ProcedureListViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientId = p.PatientId,
                    PatientName = p.Patient.PatientName,
                    PatientMRNumber = p.Patient.PatientMRNumber,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    TotalItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName,
                    PrimaryDoctorName = p.PrimaryDoctorName,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();
        }


        public async Task<ProcedureDetailsViewModel> GetProcedureDetailsAsync(Guid procedureId)
        {
            var procedure = await _context.Procedures
                .Include(p => p.Patient)
                    .ThenInclude(patient => patient.MedicalRecords.Where(mr => mr.IsActive))
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                    .ThenInclude(ia => ia.Item)
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                    .ThenInclude(iu => iu.Item)
                .Include(p => p.ClinicalReports.Where(cr => cr.IsActive))
                    .ThenInclude(cr => cr.ClinicalReport)
                .Include(p => p.ClinicalReports.Where(cr => cr.IsActive))
                    .ThenInclude(cr => cr.LinkedBy)
                .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);

            if (procedure == null) return null;

            var itemSummaries = await GetProcedureItemSummaryAsync(procedureId);

            // Get medical record and board approval data
            var medicalRecord = procedure.Patient.MedicalRecords?.FirstOrDefault();
            //  var boardApproval = medicalRecord?.BoardApproval;

            // Get previous and next procedures
            var previousProcedure = await _context.Procedures
                .Where(p => p.PatientId == procedure.PatientId && p.ProcedureDate < procedure.ProcedureDate && p.IsActive)
                .OrderByDescending(p => p.ProcedureDate)
                .Select(p => new ProcedureBasicInfoViewModel
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureStatus = p.ProcedureStatus
                })
                .FirstOrDefaultAsync();

            var nextProcedure = await _context.Procedures
                .Where(p => p.PatientId == procedure.PatientId && p.ProcedureDate > procedure.ProcedureDate && p.IsActive)
                .OrderBy(p => p.ProcedureDate)
                .Select(p => new ProcedureBasicInfoViewModel
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureStatus = p.ProcedureStatus
                })
                .FirstOrDefaultAsync();

            return new ProcedureDetailsViewModel
            {
                ProcedureId = procedure.ProcedureId,
                PatientId = procedure.PatientId,
                PatientName = procedure.Patient.PatientName,
                PatientMRNumber = procedure.Patient.PatientMRNumber,
                ProcedureDate = procedure.ProcedureDate,
                ProcedureROI = procedure.ProcedureROI,
                ProcedureDuration = procedure.ProcedureDuration,
                ProcedureEquipmentUsed = procedure.ProcedureEquipmentUsed,
                ProcedureRemarks = procedure.ProcedureRemarks,
                ProcedureStatus = procedure.ProcedureStatus,
                ProcedureNextDate = procedure.ProcedureNextDate,
                PrimaryDoctorName = procedure.PrimaryDoctorName,
                ConsultantDoctor1Name = procedure.ConsultantDoctor1Name,
                ConsultantDoctor2Name = procedure.ConsultantDoctor2Name,
                OperatingEngineerName = procedure.OperatingEngineerName,
                CreatedByName = procedure.CreatedBy?.UserName,
                CreatedAt = procedure.CreatedAt,
                ItemSummaries = itemSummaries,
                ClinicalReports = procedure.ClinicalReports.Select(cr => new ProcedureClinicalReportViewModel
                {
                    ClinicalReportId = cr.ClinicalReportId,
                    ReportType = cr.ClinicalReport.ReportType,
                    ReportDescription = cr.ClinicalReport.ReportDescription,
                    ReportFilePath = cr.ClinicalReport.ReportFilePath,
                    ReportDate = cr.ClinicalReport.ReportDate,
                    LinkedAt = cr.LinkedAt,
                    LinkedByName = cr.LinkedBy?.UserName
                }).ToList(),
                PreviousProcedure = previousProcedure,
                NextProcedure = nextProcedure,

                // Medical Record Data
                MedicalRecordCancerType = !string.IsNullOrEmpty(medicalRecord?.MedicalRecordCancerType)
                    ? medicalRecord.MedicalRecordCancerType.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                MedicalRecordCancerStage = medicalRecord?.MedicalRecordCancerStage,
                MedicalRecordComorbidities = medicalRecord?.MedicalRecordComorbidities,
                MedicalRecordTumorLocation = medicalRecord?.MedicalRecordTumorLocation,
                MedicalRecordAllergies = medicalRecord?.MedicalRecordAllergies,
                MedicalRecordClinicalHistory = medicalRecord?.MedicalRecordClinicalHistory,

                // Board Approval Data
                //BoardApprovalDate = boardApproval?.BoardApprovalDate,
                //BoardApprovalDecision = boardApproval?.BoardApprovalDecision,
                //BoardApprovalLetterNumber = boardApproval?.BoardApprovalLetterNumber,
                //BoardApprovalAttachmentPath = boardApproval?.BoardApprovalAttachmentPath
            };
        }


        //public async Task<ProcedureDetailsViewModel> GetProcedureDetailsAsync(Guid procedureId)
        //{
        //    var procedure = await _context.Procedures
        //        .Include(p => p.Patient)
        //        .Include(p => p.CreatedBy)
        //        .Include(p => p.Patient.MedicalRecords)
        //        //.Include(p => p.Patient.MedicalRecords.BoardApproval)
        //        .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
        //            .ThenInclude(ia => ia.Item)
        //        .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
        //            .ThenInclude(iu => iu.Item)
        //        .Include(p => p.ClinicalReports.Where(cr => cr.IsActive))
        //            .ThenInclude(cr => cr.ClinicalReport)
        //        .Include(p => p.ClinicalReports.Where(cr => cr.IsActive))
        //            .ThenInclude(cr => cr.LinkedBy)
        //        .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);

        //    if (procedure == null) return null;

        //    var itemSummaries = await GetProcedureItemSummaryAsync(procedureId);

        //    // Get previous and next procedures
        //    var previousProcedure = await _context.Procedures
        //        .Where(p => p.PatientId == procedure.PatientId && p.ProcedureDate < procedure.ProcedureDate && p.IsActive)
        //        .OrderByDescending(p => p.ProcedureDate)
        //        .Select(p => new ProcedureBasicInfoViewModel
        //        {
        //            ProcedureId = p.ProcedureId,
        //            ProcedureDate = p.ProcedureDate,
        //            ProcedureROI = p.ProcedureROI,
        //            ProcedureStatus = p.ProcedureStatus
        //        })
        //        .FirstOrDefaultAsync();

        //    var nextProcedure = await _context.Procedures
        //        .Where(p => p.PatientId == procedure.PatientId && p.ProcedureDate > procedure.ProcedureDate && p.IsActive)
        //        .OrderBy(p => p.ProcedureDate)
        //        .Select(p => new ProcedureBasicInfoViewModel
        //        {
        //            ProcedureId = p.ProcedureId,
        //            ProcedureDate = p.ProcedureDate,
        //            ProcedureROI = p.ProcedureROI,
        //            ProcedureStatus = p.ProcedureStatus
        //        })
        //        .FirstOrDefaultAsync();

        //    return new ProcedureDetailsViewModel
        //    {
        //        ProcedureId = procedure.ProcedureId,
        //        PatientId = procedure.PatientId,
        //        PatientName = procedure.Patient.PatientName,
        //        PatientMRNumber = procedure.Patient.PatientMRNumber,
        //        ProcedureDate = procedure.ProcedureDate,
        //        ProcedureROI = procedure.ProcedureROI,
        //        ProcedureDuration = procedure.ProcedureDuration,
        //        ProcedureEquipmentUsed = procedure.ProcedureEquipmentUsed,
        //        ProcedureRemarks = procedure.ProcedureRemarks,
        //        ProcedureStatus = procedure.ProcedureStatus,
        //        ProcedureNextDate = procedure.ProcedureNextDate,
        //        CreatedByName = procedure.CreatedBy?.UserName,
        //        CreatedAt = procedure.CreatedAt,
        //        ItemSummaries = itemSummaries,
        //        ClinicalReports = procedure.ClinicalReports.Select(cr => new ProcedureClinicalReportViewModel
        //        {
        //            ClinicalReportId = cr.ClinicalReportId,
        //            ReportType = cr.ClinicalReport.ReportType,
        //            ReportDescription = cr.ClinicalReport.ReportDescription,
        //            ReportFilePath = cr.ClinicalReport.ReportFilePath,
        //            ReportDate = cr.ClinicalReport.ReportDate,
        //            LinkedAt = cr.LinkedAt,
        //            LinkedByName = cr.LinkedBy?.UserName
        //        }).ToList(),
        //        PreviousProcedure = previousProcedure,
        //        NextProcedure = nextProcedure
        //    };
        //}

        public async Task<Procedure> GetProcedureByIdAsync(Guid procedureId)
        {
            return await _context.Procedures
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);
        }

        public async Task<bool> CreateProcedureAsync(ProcedureCreateViewModel model, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Main Procedure
                var procedure = new Procedure
                {
                    ProcedureId = Guid.NewGuid(),
                    PatientId = model.PatientId,
                    ProcedureDate = model.ProcedureDate,
                    ProcedureROI = model.ProcedureROI.Trim(),
                    ProcedureDuration = model.ProcedureDuration.Trim(),
                    ProcedureEquipmentUsed = model.ProcedureEquipmentUsed?.Trim(),
                    ProcedureRemarks = model.ProcedureRemarks?.Trim(),
                    ProcedureStatus = model.ProcedureStatus,
                    ProcedureNextDate = model.ProcedureNextDate,
                    PreviousProcedureId = model.PreviousProcedureId,

                    // NEW FIELDS - Doctor & Engineer Names
                    PrimaryDoctorName = model.PrimaryDoctorName,
                    ConsultantDoctor1Name = model.ConsultantDoctor1Name,
                    ConsultantDoctor2Name = model.ConsultantDoctor2Name,
                    OperatingEngineerName = model.OperatingEngineerName,

                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Procedures.Add(procedure);
                await _context.SaveChangesAsync();

                // 2. ============ MEDICAL RECORD - YEH CHANGE HUA HAI ============
                if (HasMedicalRecordData(model))
                {
                    // Check if patient already has a medical record
                    var existingMedicalRecord = await _context.MedicalRecords
                        .FirstOrDefaultAsync(mr => mr.PatientId == model.PatientId && mr.IsActive);

                    if (existingMedicalRecord != null)
                    {
                        // UPDATE existing medical record (NEW LOGIC)
                        existingMedicalRecord.MedicalRecordCancerType = model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()
                            ? string.Join(",", model.MedicalRecordCancerType)
                            : existingMedicalRecord.MedicalRecordCancerType;
                        existingMedicalRecord.MedicalRecordCancerStage = model.MedicalRecordCancerStage ?? existingMedicalRecord.MedicalRecordCancerStage;
                        existingMedicalRecord.MedicalRecordComorbidities = model.MedicalRecordComorbidities ?? existingMedicalRecord.MedicalRecordComorbidities;
                        existingMedicalRecord.MedicalRecordTumorLocation = model.MedicalRecordTumorLocation ?? existingMedicalRecord.MedicalRecordTumorLocation;
                        existingMedicalRecord.MedicalRecordAllergies = model.MedicalRecordAllergies ?? existingMedicalRecord.MedicalRecordAllergies;
                        existingMedicalRecord.MedicalRecordClinicalHistory = model.MedicalRecordClinicalHistory ?? existingMedicalRecord.MedicalRecordClinicalHistory;

                        _context.MedicalRecords.Update(existingMedicalRecord);
                    }
                    else
                    {
                        // Create new medical record only if none exists
                        var medicalRecord = new MedicalRecord
                        {
                            MedicalRecordId = Guid.NewGuid(),
                            PatientId = model.PatientId,
                            MedicalRecordCancerType = model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()
                                ? string.Join(",", model.MedicalRecordCancerType)
                                : null,
                            MedicalRecordCancerStage = model.MedicalRecordCancerStage,
                            MedicalRecordComorbidities = model.MedicalRecordComorbidities,
                            MedicalRecordTumorLocation = model.MedicalRecordTumorLocation,
                            MedicalRecordAllergies = model.MedicalRecordAllergies,
                            MedicalRecordClinicalHistory = model.MedicalRecordClinicalHistory,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.MedicalRecords.Add(medicalRecord);
                    }
                }

                // 4. ============ CLINICAL REPORTS - YEH CHANGE HUA HAI ============
                if (model.ClinicalReports != null && model.ClinicalReports.Any())
                {
                    foreach (var report in model.ClinicalReports)
                    {
                        // ✅ CRITICAL: Only process if there's a NEW file upload
                        if (report.ReportFile != null)  // YEH LINE ADD HUI HAI
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "clinical-reports");
                            Directory.CreateDirectory(uploadsFolder);

                            var uniqueFileName = $"{Guid.NewGuid()}_{report.ReportFile.FileName}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await report.ReportFile.CopyToAsync(stream);
                            }

                            var clinicalReport = new ClinicalReport
                            {
                                ClinicalReportId = Guid.NewGuid(),
                                PatientId = model.PatientId,
                                ReportType = report.ReportType,
                                ReportDescription = null,
                                ReportFilePath = $"/uploads/clinical-reports/{uniqueFileName}",
                                ReportDate = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            };
                            _context.ClinicalReports.Add(clinicalReport);

                            // Link to procedure
                            var procedureClinicalReport = new ProcedureClinicalReport
                            {
                                ProcedureClinicalReportId = Guid.NewGuid(),
                                ProcedureId = procedure.ProcedureId,
                                ClinicalReportId = clinicalReport.ClinicalReportId,
                                LinkedAt = DateTime.UtcNow,
                                LinkedById = userId,
                                IsActive = true
                            };
                            _context.ProcedureClinicalReports.Add(procedureClinicalReport);
                        }
                    }
                }
                // 5. Add Item Assignments if any
                if (model.ItemAssignments?.Any() == true)
                {
                    foreach (var itemAssignment in model.ItemAssignments.Where(ia => ia.QuantityAssigned > 0))
                    {
                        var assignment = new ProcedureItemAssignment
                        {
                            AssignmentId = Guid.NewGuid(),
                            ProcedureId = procedure.ProcedureId,
                            ItemId = itemAssignment.ItemId,
                            QuantityAssigned = itemAssignment.QuantityAssigned,
                            AssignmentReason = itemAssignment.AssignmentReason?.Trim() ?? "Procedure Requirement",
                            Notes = itemAssignment.Notes?.Trim(),
                            AssignedById = userId,
                            AssignedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.ProcedureItemAssignments.Add(assignment);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Log successful procedure creation
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "CREATE",
                    tableName: "Procedures",
                    recordId: procedure.ProcedureId.ToString(),
                    newValues: new { ProcedureId = procedure.ProcedureId, PatientId = procedure.PatientId, ProcedureDate = procedure.ProcedureDate, ProcedureStatus = procedure.ProcedureStatus },
                    customMessage: $"Procedure created for patient {model.PatientId}"
                );
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "CreateProcedureAsync",
                    customMessage: "Failed to create procedure",
                    additionalData: new { PatientId = model.PatientId, ProcedureDate = model.ProcedureDate },
                    serviceName: "ProcedureService"
                );
                return false;
            }
        }


        public async Task<bool> UpdateProcedureAsync(ProcedureEditViewModel model)
        {
            try
            {
                var procedure = await _context.Procedures.FindAsync(model.ProcedureId);
                if (procedure == null) return false;
                procedure.ProcedureDate = model.ProcedureDate;
                procedure.ProcedureROI = model.ProcedureROI.Trim();
                procedure.ProcedureDuration = model.ProcedureDuration.Trim();
                procedure.ProcedureEquipmentUsed = model.ProcedureEquipmentUsed?.Trim();
                procedure.ProcedureRemarks = model.ProcedureRemarks.Trim();
                procedure.ProcedureStatus = model.ProcedureStatus;
                procedure.ProcedureNextDate = model.ProcedureNextDate;
                procedure.PreviousProcedureId = model.PreviousProcedureId;


                procedure.IsActive = model.IsActive;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // UPDATED METHOD: Enhanced update with medical record and file handling
        public async Task<bool> UpdateProcedureAsync(ProcedureEditViewModel model, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update Main Procedure
                var procedure = await _context.Procedures.FindAsync(model.ProcedureId);
                if (procedure == null) return false;

                procedure.ProcedureDate = model.ProcedureDate;
                procedure.ProcedureROI = model.ProcedureROI.Trim();
                procedure.ProcedureDuration = model.ProcedureDuration.Trim();
                procedure.ProcedureEquipmentUsed = model.ProcedureEquipmentUsed?.Trim();
                procedure.ProcedureRemarks = model.ProcedureRemarks?.Trim();
                procedure.ProcedureStatus = model.ProcedureStatus;
                procedure.ProcedureNextDate = model.ProcedureNextDate;
                procedure.PreviousProcedureId = model.PreviousProcedureId;
                procedure.PrimaryDoctorName = model.PrimaryDoctorName;
                procedure.ConsultantDoctor1Name = model.ConsultantDoctor1Name;
                procedure.ConsultantDoctor2Name = model.ConsultantDoctor2Name;
                procedure.OperatingEngineerName = model.OperatingEngineerName;
                procedure.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                // 2. Update or Create Medical Record
                var existingMedicalRecord = await _context.MedicalRecords

                    .FirstOrDefaultAsync(mr => mr.PatientId == model.PatientId && mr.IsActive);

                if (HasMedicalRecordData(model))
                {
                    if (existingMedicalRecord == null)
                    {
                        // Create new medical record
                        existingMedicalRecord = new MedicalRecord
                        {
                            MedicalRecordId = Guid.NewGuid(),
                            PatientId = model.PatientId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.MedicalRecords.Add(existingMedicalRecord);
                    }

                    // Update medical record data
                    existingMedicalRecord.MedicalRecordCancerType = model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()
                        ? string.Join(",", model.MedicalRecordCancerType)
                        : null;
                    existingMedicalRecord.MedicalRecordCancerStage = model.MedicalRecordCancerStage?.Trim();
                    existingMedicalRecord.MedicalRecordComorbidities = model.MedicalRecordComorbidities?.Trim();
                    existingMedicalRecord.MedicalRecordTumorLocation = model.MedicalRecordTumorLocation?.Trim();
                    existingMedicalRecord.MedicalRecordAllergies = model.MedicalRecordAllergies?.Trim();
                    existingMedicalRecord.MedicalRecordClinicalHistory = model.MedicalRecordClinicalHistory?.Trim();

                    await _context.SaveChangesAsync();


                }

                // 4. Update Clinical Reports
                if (model.ClinicalReports != null && model.ClinicalReports.Any())
                {
                    // Get existing clinical reports for this procedure
                    var existingReports = await _context.ProcedureClinicalReports
                        .Where(pcr => pcr.ProcedureId == model.ProcedureId && pcr.IsActive)
                        .Include(pcr => pcr.ClinicalReport)
                        .ToListAsync();

                    // Mark existing reports as inactive (soft delete)
                    foreach (var existingReport in existingReports)
                    {
                        existingReport.IsActive = false;
                    }

                    // Add new clinical reports
                    foreach (var reportModel in model.ClinicalReports.Where(cr => !string.IsNullOrEmpty(cr.ReportType)))
                    {
                        // Create new clinical report
                        var clinicalReport = new ClinicalReport
                        {
                            ClinicalReportId = Guid.NewGuid(),
                            ReportType = reportModel.ReportType.Trim(),
                            ReportDescription = $"Updated report for procedure: {model.ProcedureROI}",
                            ReportDate = DateTime.Now.Date,
                            PatientId = model.PatientId,
                            ReportFilePath = reportModel.ExistingFilePath, // Keep existing if no new file
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        // Handle file upload for clinical report
                        if (reportModel.ReportFile != null && reportModel.ReportFile.Length > 0)
                        {
                            var uploadsFolder = Path.Combine("wwwroot", "uploads", "clinical-reports");
                            Directory.CreateDirectory(uploadsFolder);

                            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(reportModel.ReportFile.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await reportModel.ReportFile.CopyToAsync(fileStream);
                            }

                            clinicalReport.ReportFilePath = $"/uploads/clinical-reports/{fileName}";
                        }

                        _context.ClinicalReports.Add(clinicalReport);
                        await _context.SaveChangesAsync();

                        // Link clinical report to procedure
                        var procedureClinicalReport = new ProcedureClinicalReport
                        {
                            ProcedureClinicalReportId = Guid.NewGuid(),
                            ProcedureId = model.ProcedureId,
                            ClinicalReportId = clinicalReport.ClinicalReportId,
                            LinkedById = userId,
                            LinkedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.ProcedureClinicalReports.Add(procedureClinicalReport);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception here
                // _logger.LogError(ex, "Error updating procedure {ProcedureId}", model.ProcedureId);
                return false;
            }
        }

        // NEW METHOD: Get procedure for edit with all related data
        public async Task<Procedure> GetProcedureForEditAsync(Guid procedureId)
        {
            return await _context.Procedures
                .Include(p => p.Patient)
                    .ThenInclude(patient => patient.MedicalRecords.Where(mr => mr.IsActive))

                .Include(p => p.ClinicalReports.Where(cr => cr.IsActive))
                    .ThenInclude(cr => cr.ClinicalReport)
                .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);
        }





        public async Task<bool> DeleteProcedureAsync(Guid procedureId)
        {
            try
            {
                var procedure = await _context.Procedures.FindAsync(procedureId);
                if (procedure == null) return false;

                procedure.IsActive = false;
                await _context.SaveChangesAsync();
                // Log successful deletion
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "DELETE",
                    tableName: "Procedures",
                    recordId: procedureId.ToString(),
                    customMessage: $"Procedure soft deleted (IsActive = false) and patient id {procedure.PatientId}"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Item Assignment & Usage

        public async Task<bool> AssignItemToProcedureAsync(Guid procedureId, Guid itemId, int quantity, string reason, string? notes, string userId)
        {
            try
            {
                // Check if item already assigned
                var existingAssignment = await _context.ProcedureItemAssignments
                    .FirstOrDefaultAsync(pia => pia.ProcedureId == procedureId && pia.ItemId == itemId && pia.IsActive);

                if (existingAssignment != null)
                {
                    // Update existing assignment
                    existingAssignment.QuantityAssigned += quantity;
                    existingAssignment.Notes = notes?.Trim();
                }
                else
                {
                    // Create new assignment
                    var assignment = new ProcedureItemAssignment
                    {
                        AssignmentId = Guid.NewGuid(),
                        ProcedureId = procedureId,
                        ItemId = itemId,
                        QuantityAssigned = quantity,
                        AssignmentReason = reason.Trim(),
                        Notes = notes?.Trim(),
                        AssignedById = userId,
                        AssignedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.ProcedureItemAssignments.Add(assignment);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AssignmentResultViewModel> AssignMultipleItemsToProcedureAsync(
    Guid procedureId,
    List<AssignItemViewModel> items,
    string? overallNotes,
    string userId)
        {
            var result = new AssignmentResultViewModel();

            if (items == null || !items.Any())
            {
                result.Success = false;
                result.ErrorMessages.Add("No items provided for assignment");
                return result;
            }

            // Validate procedure exists and is not completed/cancelled
            var procedure = await _context.Procedures
                .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);

            if (procedure == null)
            {
                result.Success = false;
                result.ErrorMessages.Add("Procedure not found");
                return result;
            }

            if (procedure.ProcedureStatus == "Completed" || procedure.ProcedureStatus == "Cancelled")
            {
                result.Success = false;
                result.ErrorMessages.Add($"Cannot assign items to {procedure.ProcedureStatus} procedure");
                return result;
            }

            // Process each item
            foreach (var item in items)
            {
                try
                {
                    // Validate item exists and is active
                    var itemExists = await _context.Items
                        .AnyAsync(i => i.ItemId == item.ItemId && i.IsActive);

                    if (!itemExists)
                    {
                        result.FailCount++;
                        result.ErrorMessages.Add($"Item {item.ItemId} not found or inactive");
                        continue;
                    }

                    // Get item details for logging
                    var itemDetails = await _context.Items
                        .FirstOrDefaultAsync(i => i.ItemId == item.ItemId);

                    // Check if sufficient stock available (optional check)
                    var currentStock = await GetCurrentStockAsync2(item.ItemId);
                    if (currentStock < item.Quantity)
                    {
                        result.FailCount++;
                        result.ErrorMessages.Add($"{itemDetails?.ItemName}: Insufficient stock (Available: {currentStock}, Required: {item.Quantity})");
                        continue;
                    }

                    // Check if item already assigned to this procedure
                    var existingAssignment = await _context.ProcedureItemAssignments
                        .FirstOrDefaultAsync(pia =>
                            pia.ProcedureId == procedureId &&
                            pia.ItemId == item.ItemId &&
                            pia.IsActive);

                    if (existingAssignment != null)
                    {
                        // Update existing assignment - add to quantity
                        existingAssignment.QuantityAssigned += item.Quantity;

                        // Update notes if overall notes provided
                        if (!string.IsNullOrWhiteSpace(overallNotes))
                        {
                            existingAssignment.Notes = string.IsNullOrWhiteSpace(existingAssignment.Notes)
                                ? overallNotes.Trim()
                                : $"{existingAssignment.Notes}; {overallNotes.Trim()}";
                        }

                        await _context.SaveChangesAsync();

                        result.SuccessCount++;
                        result.SuccessMessages.Add($"{itemDetails?.ItemName}: Quantity updated (added {item.Quantity})");

                        // Log update
                        await _auditLogService.LogDatabaseOperationAsync(
                            actionType: "UPDATE_ASSIGNMENT",
                            tableName: "ProcedureItemAssignments",
                            recordId: existingAssignment.AssignmentId.ToString(),
                            oldValues: new { OldQuantity = existingAssignment.QuantityAssigned - item.Quantity },
                            newValues: new { NewQuantity = existingAssignment.QuantityAssigned, ItemAdded = item.Quantity },
                            customMessage: $"Updated assignment for {itemDetails?.ItemName} in procedure {procedureId}"
                        );
                    }
                    else
                    {
                        // Create new assignment
                        var assignment = new ProcedureItemAssignment
                        {
                            AssignmentId = Guid.NewGuid(),
                            ProcedureId = procedureId,
                            ItemId = item.ItemId,
                            QuantityAssigned = item.Quantity,
                            AssignmentReason = item.Reason.Trim(),
                            Notes = overallNotes?.Trim(),
                            AssignedById = userId,
                            AssignedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.ProcedureItemAssignments.Add(assignment);
                        await _context.SaveChangesAsync();

                        result.SuccessCount++;
                        result.SuccessMessages.Add($"{itemDetails?.ItemName}: Assigned successfully ({item.Quantity} units)");

                        // Log creation
                        await _auditLogService.LogDatabaseOperationAsync(
                            actionType: "CREATE_ASSIGNMENT",
                            tableName: "ProcedureItemAssignments",
                            recordId: assignment.AssignmentId.ToString(),
                            newValues: new
                            {
                                AssignmentId = assignment.AssignmentId,
                                ProcedureId = procedureId,
                                ItemId = item.ItemId,
                                ItemName = itemDetails?.ItemName,
                                Quantity = item.Quantity,
                                Reason = item.Reason
                            },
                            customMessage: $"Assigned {itemDetails?.ItemName} to procedure {procedureId}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.ErrorMessages.Add($"Error processing item {item.ItemId}: {ex.Message}");

                    // Log exception
                    await _auditLogService.LogExceptionAsync(
                        exception: ex,
                        methodName: "AssignMultipleItemsToProcedureAsync",
                        customMessage: $"Failed to assign item {item.ItemId} to procedure {procedureId}",
                        additionalData: new { ProcedureId = procedureId, ItemId = item.ItemId, Quantity = item.Quantity },
                        serviceName: "ProcedureService"
                    );
                }
            }

            // Set overall success based on counts
            result.Success = result.SuccessCount > 0;

            return result;
        }

        public async Task<IEnumerable<ProcedureItemAssignment>> GetAssignedItemsAsync(Guid procedureId)
        {
            return await _context.ProcedureItemAssignments
                .Where(pia => pia.ProcedureId == procedureId && pia.IsActive)
                .Include(pia => pia.Item)
                .Include(pia => pia.AssignedBy)
                .OrderBy(pia => pia.AssignedDate)
                .ToListAsync();
        }


        public async Task<AssignmentResultViewModel> UpdateAssignedItemsAsync(
      Guid procedureId,
      List<EditAssignItemViewModel> items,
      string? overallNotes,
      string userId)
        {
            var result = new AssignmentResultViewModel();

            if (items == null || !items.Any())
            {
                result.Success = false;
                result.ErrorMessages.Add("No items provided");
                return result;
            }

            // Validate procedure
            var procedure = await _context.Procedures
                .FirstOrDefaultAsync(p => p.ProcedureId == procedureId && p.IsActive);

            if (procedure == null)
            {
                result.Success = false;
                result.ErrorMessages.Add("Procedure not found");
                return result;
            }

            if (procedure.ProcedureStatus == "Completed" || procedure.ProcedureStatus == "Cancelled")
            {
                result.Success = false;
                result.ErrorMessages.Add($"Cannot edit items for {procedure.ProcedureStatus} procedure");
                return result;
            }

            // Process each item
            foreach (var item in items)
            {
                try
                {
                    // Handle deletion
                    if (item.IsDeleted && item.AssignmentId.HasValue)
                    {
                        var assignmentToDelete = await _context.ProcedureItemAssignments
                            .Include(a => a.Item)
                            .FirstOrDefaultAsync(pia => pia.AssignmentId == item.AssignmentId.Value);

                        if (assignmentToDelete != null)
                        {
                            // Check if used in THIS procedure
                            var usedQuantity = await _context.ProcedureItemUsages
                                .Where(piu =>
                                    piu.ProcedureId == procedureId &&
                                    piu.ItemId == assignmentToDelete.ItemId &&
                                    piu.IsActive)
                                .SumAsync(piu => (int?)piu.QuantityUsed) ?? 0;

                            if (usedQuantity > 0)
                            {
                                result.FailCount++;
                                result.ErrorMessages.Add($"{assignmentToDelete.Item?.ItemName}: Cannot delete - {usedQuantity} unit(s) already used in this procedure");
                                continue;
                            }

                            // Soft delete
                            assignmentToDelete.IsActive = false;
                            await _context.SaveChangesAsync();

                            result.SuccessCount++;
                            result.SuccessMessages.Add($"{assignmentToDelete.Item?.ItemName}: Removed successfully");

                            await _auditLogService.LogDatabaseOperationAsync(
                                "DELETE_ASSIGNMENT",
                                "ProcedureItemAssignments",
                                item.AssignmentId.Value.ToString(),
                                customMessage: $"Deleted assignment for {assignmentToDelete.Item?.ItemName}"
                            );
                        }
                        else
                        {
                            result.FailCount++;
                            result.ErrorMessages.Add("Assignment not found");
                        }
                        continue;
                    }

                    // Skip deleted items without AssignmentId
                    if (item.IsDeleted)
                        continue;

                    // Validate item exists and is active
                    var itemDetails = await _context.Items
                        .FirstOrDefaultAsync(i => i.ItemId == item.ItemId && i.IsActive);

                    if (itemDetails == null)
                    {
                        result.FailCount++;
                        result.ErrorMessages.Add($"Item not found or inactive");
                        continue;
                    }

                    // Check stock availability
                    var currentStock = await GetCurrentStockAsync(item.ItemId);

                    int adjustedStock = currentStock;
                    if (item.AssignmentId.HasValue)
                    {
                        var existingQty = await _context.ProcedureItemAssignments
                            .Where(a => a.AssignmentId == item.AssignmentId.Value)
                            .Select(a => a.QuantityAssigned)
                            .FirstOrDefaultAsync();
                        adjustedStock += existingQty;
                    }

                    if (adjustedStock < item.Quantity)
                    {
                        result.FailCount++;
                        result.ErrorMessages.Add($"{itemDetails.ItemName}: Insufficient stock (Available: {adjustedStock}, Required: {item.Quantity})");
                        continue;
                    }

                    // Update existing assignment
                    if (item.AssignmentId.HasValue)
                    {
                        var existingAssignment = await _context.ProcedureItemAssignments
                            .FirstOrDefaultAsync(pia => pia.AssignmentId == item.AssignmentId.Value);

                        if (existingAssignment != null)
                        {
                            var oldQuantity = existingAssignment.QuantityAssigned;
                            existingAssignment.QuantityAssigned = item.Quantity;
                            existingAssignment.AssignmentReason = item.Reason.Trim();

                            if (!string.IsNullOrWhiteSpace(overallNotes))
                            {
                                existingAssignment.Notes = overallNotes.Trim();
                            }

                            await _context.SaveChangesAsync();

                            result.SuccessCount++;
                            result.SuccessMessages.Add($"{itemDetails.ItemName}: Updated (Qty: {oldQuantity} → {item.Quantity})");

                            await _auditLogService.LogDatabaseOperationAsync(
                                "UPDATE_ASSIGNMENT",
                                "ProcedureItemAssignments",
                                item.AssignmentId.Value.ToString(),
                                oldValues: new { OldQuantity = oldQuantity },
                                newValues: new { NewQuantity = item.Quantity },
                                customMessage: $"Updated assignment for {itemDetails.ItemName}"
                            );
                        }
                        else
                        {
                            result.FailCount++;
                            result.ErrorMessages.Add($"{itemDetails.ItemName}: Assignment not found for update");
                        }
                    }
                    // Create new assignment
                    else
                    {
                        var newAssignment = new ProcedureItemAssignment
                        {
                            AssignmentId = Guid.NewGuid(),
                            ProcedureId = procedureId,
                            ItemId = item.ItemId,
                            QuantityAssigned = item.Quantity,
                            AssignmentReason = item.Reason.Trim(),
                            Notes = overallNotes?.Trim(),
                            AssignedById = userId,
                            AssignedDate = DateTime.UtcNow,
                            IsActive = true
                        };

                        _context.ProcedureItemAssignments.Add(newAssignment);
                        await _context.SaveChangesAsync();

                        result.SuccessCount++;
                        result.SuccessMessages.Add($"{itemDetails.ItemName}: Added successfully ({item.Quantity} units)");

                        await _auditLogService.LogDatabaseOperationAsync(
                            "CREATE_ASSIGNMENT",
                            "ProcedureItemAssignments",
                            newAssignment.AssignmentId.ToString(),
                            newValues: new { ItemId = item.ItemId, Quantity = item.Quantity },
                            customMessage: $"Added new assignment for {itemDetails.ItemName}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.ErrorMessages.Add($"Error processing item: {ex.Message}");

                    await _auditLogService.LogExceptionAsync(
                        exception: ex,
                        methodName: "UpdateAssignedItemsAsync",
                        customMessage: $"Failed to update item in procedure {procedureId}",
                        additionalData: new { ProcedureId = procedureId, ItemId = item.ItemId },
                        serviceName: "ProcedureService"
                    );
                }
            }

            result.Success = result.SuccessCount > 0;
            return result;
        }

        // Helper method for getting current stock (agar already nahi hai to)

        private async Task<int> GetCurrentStockAsync2(Guid itemId)
        {
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .SumAsync(sr => sr.QuantityReceived);

            var totalUsed = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .SumAsync(ur => ur.QuantityUsed);

            

            return totalReceived - totalUsed ;
        }


        private async Task<int> GetCurrentStockAsync(Guid itemId)
        {
            var totalReceived = await _context.StockRecords
                .Where(sr => sr.ItemId == itemId && sr.IsActive)
                .SumAsync(sr => sr.QuantityReceived);

            var totalUsed = await _context.UsageRecords
                .Where(ur => ur.ItemId == itemId && ur.IsActive)
                .SumAsync(ur => ur.QuantityUsed);

            var totalUsedInProcedures = await _context.ProcedureItemUsages
                .Where(piu => piu.ItemId == itemId && piu.IsActive)
                .SumAsync(piu => piu.QuantityUsed);

            return totalReceived - totalUsed - totalUsedInProcedures;
        }

        public async Task<bool> RecordItemUsageAsync(ProcedureItemUsageViewModel model, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate usage against assignment
                var isValid = await ValidateItemUsageAsync(model.ProcedureId, model.ItemId, model.QuantityUsed);
                if (!isValid) return false;

                // Check inventory availability
                var currentStock = await _inventoryService.GetCurrentStockAsync(model.ItemId);
                if (currentStock < model.QuantityUsed) return false;

                // Record usage
                var usage = new ProcedureItemUsage
                {
                    UsageId = Guid.NewGuid(),
                    ProcedureId = model.ProcedureId,
                    ItemId = model.ItemId,
                    QuantityUsed = model.QuantityUsed,
                    UsageDateTime = model.UsageDateTime,
                    UsageNotes = model.UsageNotes?.Trim(),
                    RecordedById = userId,
                    RecordedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ProcedureItemUsages.Add(usage);

                // Update inventory via existing inventory service
                var usageRecord = new UsageRecord
                {
                    UsageRecordId = Guid.NewGuid(),
                    ItemId = model.ItemId,
                    QuantityUsed = model.QuantityUsed,
                    UsageDate = model.UsageDateTime,
                    UsageReason = "Procedure",
                    ProcedureId = model.ProcedureId,
                    Notes = $"Used in procedure: {model.UsageNotes}",
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UsageRecords.Add(usageRecord);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> RemoveItemAssignmentAsync(Guid assignmentId)
        {
            try
            {
                var assignment = await _context.ProcedureItemAssignments.FindAsync(assignmentId);
                if (assignment == null) return false;

                // Check if any usage recorded
                var hasUsage = await _context.ProcedureItemUsages
                    .AnyAsync(iu => iu.ProcedureId == assignment.ProcedureId && iu.ItemId == assignment.ItemId && iu.IsActive);

                if (hasUsage) return false; // Cannot remove assignment if items already used

                assignment.IsActive = false;
                await _context.SaveChangesAsync();
                // Log item assignment removal
                await _auditLogService.LogAsync(
                    actionType: "REMOVE_ITEM_ASSIGNMENT",
                    message: "Item assignment removed from procedure",
                    recordId: assignment.ProcedureId.ToString(),
                    tableName: "ProcedureItemAssignments",
                    additionalData: new { AssignmentId = assignmentId, ProcedureId = assignment.ProcedureId, ItemId = assignment.ItemId },
                    serviceName: "ProcedureService",
                    methodName: "RemoveItemAssignmentAsync"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProcedureItemSummaryViewModel>> GetProcedureItemSummaryAsync(Guid procedureId)
        {
            var assignments = await _context.ProcedureItemAssignments
                .Where(pia => pia.ProcedureId == procedureId && pia.IsActive)
                .Include(pia => pia.Item)
                .ToListAsync();

            var usages = await _context.ProcedureItemUsages
                .Where(piu => piu.ProcedureId == procedureId && piu.IsActive)
                .GroupBy(piu => piu.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalUsed = g.Sum(x => x.QuantityUsed),
                    LastUsageDate = g.Max(x => x.UsageDateTime)
                })
                .ToListAsync();

            return assignments.Select(a => new ProcedureItemSummaryViewModel
            {
                ItemId = a.ItemId,
                ItemName = a.Item.ItemName,
                ItemCode = a.Item.ItemCode,
                Unit = a.Item.Unit,
                QuantityAssigned = a.QuantityAssigned,
                QuantityUsed = usages.FirstOrDefault(u => u.ItemId == a.ItemId)?.TotalUsed ?? 0,
                AssignmentReason = a.AssignmentReason,
                AssignedDate = a.AssignedDate,
                LastUsageDate = usages.FirstOrDefault(u => u.ItemId == a.ItemId)?.LastUsageDate
            }).ToList();
        }

        #endregion

        #region Clinical Reports

        public async Task<bool> LinkClinicalReportAsync(Guid procedureId, Guid clinicalReportId, string userId)
        {
            try
            {
                // Check if already linked
                var existingLink = await _context.ProcedureClinicalReports
                    .FirstOrDefaultAsync(pcr => pcr.ProcedureId == procedureId && pcr.ClinicalReportId == clinicalReportId && pcr.IsActive);

                if (existingLink != null) return false; // Already linked

                var link = new ProcedureClinicalReport
                {
                    ProcedureClinicalReportId = Guid.NewGuid(),
                    ProcedureId = procedureId,
                    ClinicalReportId = clinicalReportId,
                    LinkedById = userId,
                    LinkedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ProcedureClinicalReports.Add(link);
                await _context.SaveChangesAsync();
                // Log clinical report link
                await _auditLogService.LogAsync(
                    actionType: "LINK_CLINICAL_REPORT",
                    message: "Clinical report linked to procedure",
                    recordId: procedureId.ToString(),
                    tableName: "ProcedureClinicalReports",
                    additionalData: new { ProcedureId = procedureId, ClinicalReportId = clinicalReportId },
                    serviceName: "ProcedureService",
                    methodName: "LinkClinicalReportAsync"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UnlinkClinicalReportAsync(Guid procedureId, Guid clinicalReportId)
        {
            try
            {
                var link = await _context.ProcedureClinicalReports
                    .FirstOrDefaultAsync(pcr => pcr.ProcedureId == procedureId && pcr.ClinicalReportId == clinicalReportId && pcr.IsActive);

                if (link == null) return false;

                link.IsActive = false;
                await _context.SaveChangesAsync();
                // Log clinical report unlink
                await _auditLogService.LogAsync(
                    actionType: "UNLINK_CLINICAL_REPORT",
                    message: "Clinical report unlinked from procedure",
                    recordId: procedureId.ToString(),
                    tableName: "ProcedureClinicalReports",
                    additionalData: new { ProcedureId = procedureId, ClinicalReportId = clinicalReportId },
                    serviceName: "ProcedureService",
                    methodName: "UnlinkClinicalReportAsync"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<AvailableClinicalReportViewModel>> GetAvailableClinicalReportsAsync(Guid patientId, Guid procedureId)
        {
            var linkedReports = await _context.ProcedureClinicalReports
                .Where(pcr => pcr.ProcedureId == procedureId && pcr.IsActive)
                .Select(pcr => pcr.ClinicalReportId)
                .ToListAsync();

            var reports = await _context.ClinicalReports
                .Where(cr => cr.PatientId == patientId && cr.IsActive)
                .Include(cr => cr.Patient)
                .Select(cr => new AvailableClinicalReportViewModel
                {
                    ClinicalReportId = cr.ClinicalReportId,
                    ReportType = cr.ReportType,
                    ReportDescription = cr.ReportDescription,
                    ReportDate = cr.ReportDate,
                    IsAlreadyLinked = linkedReports.Contains(cr.ClinicalReportId)
                })
                .OrderByDescending(cr => cr.ReportDate)
                .ToListAsync();

            return reports;
        }

        #endregion

        #region Dashboard & Analytics

        public async Task<ProcedureDashboardViewModel> GetDashboardDataAsync()
        {
            var totalProcedures = await _context.Procedures.CountAsync(p => p.IsActive);
            var scheduledProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureStatus == "Scheduled");
            var inProgressProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureStatus == "In-Progress");
            var completedProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureStatus == "Completed");
            var cancelledProcedures = await _context.Procedures.CountAsync(p => p.IsActive && p.ProcedureStatus == "Cancelled");

            var upcomingProcedures = await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureDate >= DateTime.Today && p.ProcedureStatus == "Scheduled")
                .Include(p => p.Patient)
                .OrderBy(p => p.ProcedureDate)
                .Take(5)
                .Select(p => new UpcomingProcedureViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientName = p.Patient.PatientName,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDate = p.ProcedureDate,
                    Status = p.ProcedureStatus
                })
                .ToListAsync();

            var recentProcedures = await _context.Procedures
                .Where(p => p.IsActive)
                .Include(p => p.Patient)
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .OrderByDescending(p => p.ProcedureDate)
                .Take(5)
                .Select(p => new RecentProcedureViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientName = p.Patient.PatientName,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDate = p.ProcedureDate,
                    Status = p.ProcedureStatus,
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed)
                })
                .ToListAsync();

            var itemUsageSummary = await _context.ProcedureItemUsages
                .Where(piu => piu.IsActive && piu.UsageDateTime >= DateTime.Today.AddDays(-30))
                .Include(piu => piu.Item)
                .GroupBy(piu => new { piu.Item.ItemName, piu.Item.ItemCode, piu.Item.Unit })
                .Select(g => new ProcedureItemUsageSummaryViewModel
                {
                    ItemName = g.Key.ItemName,
                    ItemCode = g.Key.ItemCode,
                    Unit = g.Key.Unit,
                    TotalUsed = g.Sum(x => x.QuantityUsed),
                    ProcedureCount = g.Select(x => x.ProcedureId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalUsed)
                .Take(10)
                .ToListAsync();

            return new ProcedureDashboardViewModel
            {
                TotalProcedures = totalProcedures,
                ScheduledProcedures = scheduledProcedures,
                InProgressProcedures = inProgressProcedures,
                CompletedProcedures = completedProcedures,
                CancelledProcedures = cancelledProcedures,
                UpcomingProcedures = upcomingProcedures,
                RecentProcedures = recentProcedures,
                ItemUsageSummary = itemUsageSummary
            };
        }

        public async Task<IEnumerable<ProcedureListViewModel>> GetProceduresByPatientAsync(Guid patientId)
        {
            return await _context.Procedures
                .Where(p => p.PatientId == patientId && p.IsActive)
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .Select(p => new ProcedureListViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientId = p.PatientId,
                    PatientName = p.Patient.PatientName,
                    PatientMRNumber = p.Patient.PatientMRNumber,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    TotalItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProcedureListViewModel>> GetProceduresByStatusAsync(string status)
        {
            return await _context.Procedures
                .Where(p => p.ProcedureStatus == status && p.IsActive)
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .Select(p => new ProcedureListViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientId = p.PatientId,
                    PatientName = p.Patient.PatientName,
                    PatientMRNumber = p.Patient.PatientMRNumber,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    TotalItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();
        }

        // Add this method in ProcedureService class

        public async Task<IEnumerable<ProcedureListViewModel>> GetProceduresByStatusesAsync(string status)
        {
            return await _context.Procedures
                .Where(p => p.IsActive && p.ProcedureStatus == status)
                .Include(p => p.Patient)
                .Include(p => p.CreatedBy)
                .Include(p => p.ItemAssignments.Where(ia => ia.IsActive))
                .Include(p => p.ItemUsages.Where(iu => iu.IsActive))
                .Select(p => new ProcedureListViewModel
                {
                    ProcedureId = p.ProcedureId,
                    PatientId = p.PatientId,
                    PatientName = p.Patient.PatientName,
                    PatientMRNumber = p.Patient.PatientMRNumber,
                    ProcedureDate = p.ProcedureDate,
                    ProcedureROI = p.ProcedureROI,
                    ProcedureDuration = p.ProcedureDuration,
                    ProcedureStatus = p.ProcedureStatus,
                    TotalItemsAssigned = p.ItemAssignments.Sum(ia => ia.QuantityAssigned),
                    TotalItemsUsed = p.ItemUsages.Sum(iu => iu.QuantityUsed),
                    CreatedByName = p.CreatedBy.UserName,
                    PrimaryDoctorName = p.PrimaryDoctorName,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();
        }

        #endregion

        #region Utility Methods

        public async Task<IEnumerable<Patient>> GetActivePatientsAsync()
        {
            return await _context.Patients
                .Where(p => p.IsActive)
                .OrderBy(p => p.PatientName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAvailableItemsAsync()
        {
            return await _context.Items
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }

        public async Task<int> GetAvailableStockAsync(Guid itemId)
        {
            return await _inventoryService.GetCurrentStockAsync(itemId);
        }

        public async Task<bool> ValidateItemUsageAsync(Guid procedureId, Guid itemId, int quantityToUse)
        {
            var totalAssigned = await _context.ProcedureItemAssignments
                .Where(pia => pia.ProcedureId == procedureId && pia.ItemId == itemId && pia.IsActive)
                .SumAsync(pia => pia.QuantityAssigned);

            var totalUsed = await _context.ProcedureItemUsages
                .Where(piu => piu.ProcedureId == procedureId && piu.ItemId == itemId && piu.IsActive)
                .SumAsync(piu => piu.QuantityUsed);

            var remainingAssigned = totalAssigned - totalUsed;

            return quantityToUse <= remainingAssigned;
        }

        #endregion

        public async Task<bool> ChangeProcedureStatusAsync(Guid procedureId, string newStatus)
        {
            try
            {
                var procedure = await _context.Procedures.FindAsync(procedureId);
                if (procedure == null) return false;

                var oldStatus = procedure.ProcedureStatus;
                procedure.ProcedureStatus = newStatus;
                await _context.SaveChangesAsync();
                // Log status change
                await _auditLogService.LogDatabaseOperationAsync(
                    actionType: "STATUS_CHANGE",
                    tableName: "Procedures",
                    recordId: procedureId.ToString(),
                    oldValues: new { ProcedureStatus = oldStatus },
                    newValues: new { ProcedureStatus = newStatus },
                    customMessage: $"Procedure status changed from {oldStatus} to {newStatus}"
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CompleteProcedureAsync(Guid procedureId, string irRemarks, DateTime? dischargeDate, DateTime? nextProcedureDate, string? completionNotes)
        {
            try
            {
                var procedure = await _context.Procedures.FindAsync(procedureId);
                if (procedure == null) return false;

                // Update procedure with completion details
                procedure.ProcedureStatus = "Completed";
                procedure.ProcedureNextDate = nextProcedureDate;

                // Append completion details to remarks
                var completionDetails = $"\n\n--- COMPLETION DETAILS ---\nIR Consultant Remarks: {irRemarks}\n";
                if (dischargeDate.HasValue)
                    completionDetails += $"Discharge Date: {dischargeDate.Value:dd/MM/yyyy}\n";
                if (!string.IsNullOrEmpty(completionNotes))
                    completionDetails += $"Completion Notes: {completionNotes}";

                procedure.ProcedureRemarks = (procedure.ProcedureRemarks ?? "") + completionDetails;

                await _context.SaveChangesAsync();
                // Log procedure completion
                await _auditLogService.LogAsync(
                    actionType: "PROCEDURE_COMPLETED",
                    message: "Procedure marked as completed",
                    recordId: procedureId.ToString(),
                    tableName: "Procedures",
                    additionalData: new { ProcedureId = procedureId, DischargeDate = dischargeDate, NextProcedureDate = nextProcedureDate },
                    serviceName: "ProcedureService",
                    methodName: "CompleteProcedureAsync"
                );
                return true;
            }
            catch (Exception ex)
            {
                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "CompleteProcedureAsync",
                    customMessage: "Failed to complete procedure",
                    additionalData: new { ProcedureId = procedureId },
                    serviceName: "ProcedureService"
                );

                return false;
            }
        }
        // Helper methods
        private bool HasMedicalRecordData(ProcedureCreateViewModel model)
        {
            return !string.IsNullOrEmpty(model.MedicalRecordCancerStage) ||
                   (model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()) ||
                   !string.IsNullOrEmpty(model.MedicalRecordComorbidities) ||
                   !string.IsNullOrEmpty(model.MedicalRecordTumorLocation) ||
                   !string.IsNullOrEmpty(model.MedicalRecordAllergies) ||
                   !string.IsNullOrEmpty(model.MedicalRecordClinicalHistory);
        }

        private bool HasBoardApprovalData(ProcedureCreateViewModel model)
        {
            return model.BoardApprovalDate.HasValue ||
                   model.BoardApprovalDecision.HasValue ||
                   !string.IsNullOrEmpty(model.BoardApprovalLetterNumber) ||
                   model.BoardApprovalAttachment != null;
        }

        private bool HasMedicalRecordData(ProcedureEditViewModel model)
        {
            return !string.IsNullOrEmpty(model.MedicalRecordCancerStage) ||
                   (model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()) ||
                   !string.IsNullOrEmpty(model.MedicalRecordComorbidities) ||
                   !string.IsNullOrEmpty(model.MedicalRecordTumorLocation) ||
                   !string.IsNullOrEmpty(model.MedicalRecordAllergies) ||
                   !string.IsNullOrEmpty(model.MedicalRecordClinicalHistory);
        }
        private bool HasBoardApprovalData(ProcedureEditViewModel model)
        {
            return model.BoardApprovalDate.HasValue ||
                   model.BoardApprovalDecision.HasValue ||
                   !string.IsNullOrEmpty(model.BoardApprovalLetterNumber) ||
                   model.BoardApprovalAttachment != null;
        }


        public async Task<bool> UpdateProcedureStatusAsync(Guid procedureId, string newStatus)
        {
            try
            {
                var procedure = await _context.Procedures.FindAsync(procedureId);
                if (procedure == null)
                    return false;

                var oldStatus = procedure.ProcedureStatus;
                procedure.ProcedureStatus = newStatus;

                // Optional: Update timestamp if you have these fields
                // procedure.UpdatedAt = DateTime.Now;

                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    // Log status update
                    await _auditLogService.LogDatabaseOperationAsync(
                        actionType: "UPDATE",
                        tableName: "Procedures",
                        recordId: procedureId.ToString(),
                        oldValues: new { ProcedureStatus = oldStatus },
                        newValues: new { ProcedureStatus = newStatus },
                        customMessage: $"Procedure status updated from {oldStatus} to {newStatus}"
                    );
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                // Log error if you have logging
                return false;
            }
        }



    }
}