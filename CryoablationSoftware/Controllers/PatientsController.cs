using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CryoablationSoftware.Data;
using CryoablationSoftware.Models;
using CryoablationSoftware.ViewModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CryoablationSoftware.Services;

namespace CryoablationSoftware.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditLogService _auditLogService;
        public PatientsController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<IdentityUser> userManager, IAuditLogService auditLogService)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }
        // GET: Patients
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var patientsQuery = _context.Patients
                .Include(p => p.Procedures)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                patientsQuery = patientsQuery.Where(p =>
                    p.PatientName.ToLower().Contains(searchTerm)
                    || p.PatientCNIC.Contains(searchTerm)
                    || p.PatientPhone.Contains(searchTerm)
                    || p.PatientCity.ToLower().Contains(searchTerm));
            }

            var patients = await patientsQuery
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(patients);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var patient = await _context.Patients
                .Include(p => p.EmergencyContacts)
                .Include(p => p.ReferralInfos)
                .Include(p => p.Procedures)
                .Include(p => p.MedicalRecords)
                    .ThenInclude(mr => mr.ClinicalReports)
                .Include(p => p.MedicalRecords)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }


        // GET: Patients/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
            // Tumhare ViewModel ke mutabiq sirf lists initialize karo
            var vm = new PatientCreateViewModel
            {
                EmergencyContacts = new List<EmergencyContactViewModel>(),
                ReferralInfos = new List<ReferralInfoViewModel>(),
                ClinicalReports = new List<ClinicalReportViewModel>(),
                MedicalRecordCancerType = new List<string>() // Yeh bhi JS ya view mein use hota
            };
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientCreateViewModel model)
        {
            // 1. Server-side Model Validation
            if (!ModelState.IsValid)
            {
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Patient creation validation failed",
                    tableName: "Patients",
                    additionalData: new { PatientName = model.PatientName, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() },
                    serviceName: "PatientsController",
                    methodName: "Create"
                );

                ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
                return View(model); // Client error, form reload with error messages
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
                // Log user not found
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "User not found during patient creation",
                    tableName: "Patients",
                    additionalData: new { PatientName = model.PatientName },
                    serviceName: "PatientsController",
                    methodName: "Create"
                );

                ModelState.AddModelError("", "User not found, please login again.");
                return View(model);
            }
            // 2. UserID (CreatedById) — tumhare project me login user ka ID assign karo
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //      var userId = "PASTE_KARO_APNI_USERID_YAHAN"; // Must exist in ApplicationUser table

            // BEGIN TRANSACTION
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 3. Patient Create (pehle Patient save karo)
                var patient = new Patient
                {
                    PatientName = model.PatientName,
                    PatientCNIC = model.PatientCNIC,
                    PatientDOB = model.PatientDOB,
                    PatientMRNumber = model.PatientMRNumber,
                    PatientPhone = model.PatientPhone,
                    PatientGender = model.PatientGender,
                    PatientAge = model.PatientAge,
                    PatientAddress = model.PatientAddress,
                    PatientCity = model.PatientCity,
                    PatientProvince = model.PatientProvince,
                    PatientInsuranceInfo = model.PatientInsuranceInfo,
                    PatientPaymentMode = model.PatientPaymentMode,
                    CreatedById = userId,

                    // CreatedById = userId, // Foreign key constraint!
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync(); // PatientId mil jayega

                // 4. EmergencyContacts — PatientId set karo
                if (model.EmergencyContacts != null)
                {
                    foreach (var ecVm in model.EmergencyContacts)
                    {
                        var ec = new EmergencyContact
                        {
                            EmergencyContactName = ecVm.Name,
                            EmergencyContactCNIC = ecVm.CNIC,
                            EmergencyContactRelationship = ecVm.Relationship,
                            EmergencyContactPhone = ecVm.Phone,
                            EmergencyContactAddress = ecVm.Address,
                            EmergencyContactCity = ecVm.City,
                            EmergencyContactProvince = ecVm.Province,
                            PatientId = patient.PatientId, // Foreign key yahan!
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.EmergencyContacts.Add(ec);
                    }
                    await _context.SaveChangesAsync();
                }

                // 5. ReferralInfos — PatientId set karo
                if (model.ReferralInfos != null)
                {
                    foreach (var rVm in model.ReferralInfos)
                    {
                        var refInfo = new ReferralInfo
                        {
                            ReferralHospitalName = rVm.HospitalName,
                            ReferralCity = rVm.City,
                            ReferralDoctorName = rVm.DoctorName,
                            ReferralDepartment = rVm.Department,
                            ReferralPhone = rVm.Phone,
                            ReferralEmail = rVm.Email,
                            PatientId = patient.PatientId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.ReferralInfos.Add(refInfo);
                    }
                    await _context.SaveChangesAsync();
                }

                // 6. MedicalRecord — PatientId set karo, nullables ka dhyan
                var medRec = new MedicalRecord
                {
                    PatientId = patient.PatientId,
                    MedicalRecordCancerType = model.MedicalRecordCancerType != null ? string.Join(",", model.MedicalRecordCancerType) : null,
                    MedicalRecordCancerStage = model.MedicalRecordCancerStage,
                    MedicalRecordComorbidities = model.MedicalRecordComorbidities,
                    MedicalRecordTumorLocation = model.MedicalRecordTumorLocation,
                    MedicalRecordAllergies = model.MedicalRecordAllergies,
                    MedicalRecordClinicalHistory = model.MedicalRecordClinicalHistory,

                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.MedicalRecords.Add(medRec);
                await _context.SaveChangesAsync();

                // 7. Clinical Reports — MedicalRecordId set karo
                if (model.ClinicalReports != null)
                {
                    foreach (var cVm in model.ClinicalReports)
                    {
                        var reportFilePath = ""; // Yahan file upload ka logic lagao
                        if (cVm.ReportFile != null && cVm.ReportFile.Length > 0)
                        {
                            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "clinicalreports");
                            if (!Directory.Exists(uploadFolder))
                                Directory.CreateDirectory(uploadFolder);

                            var fileName = Guid.NewGuid() + Path.GetExtension(cVm.ReportFile.FileName);
                            var path = Path.Combine(uploadFolder, fileName);
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                await cVm.ReportFile.CopyToAsync(stream);
                            }
                            reportFilePath = "/uploads/clinicalreports/" + fileName;
                        }

                        var cr = new ClinicalReport
                        {
                            PatientId = medRec.PatientId,
                            ReportType = cVm.ReportType,
                            ReportFilePath = reportFilePath,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.ClinicalReports.Add(cr);
                    }
                    await _context.SaveChangesAsync();
                }


                // 9. Procedure — PatientId set karo
                var procedure = new Procedure
                {
                    PatientId = patient.PatientId,
                    ProcedureDate = model.ProcedureDate ?? DateTime.UtcNow,
                    ProcedureDuration = model.ProcedureDuration,
                    ProcedureROI = model.ProcedureROI,
                    ProcedureRemarks = model.ProcedureRemarks,

                    // NEW FIELDS - Doctor & Engineer Names
                    PrimaryDoctorName = model.PrimaryDoctorName,
                    ConsultantDoctor1Name = model.ConsultantDoctor1Name,
                    ConsultantDoctor2Name = model.ConsultantDoctor2Name,
                    OperatingEngineerName = model.OperatingEngineerName,
                    ProcedureStatus = model.ProcedureStatus,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedById = userId
                };
                _context.Procedures.Add(procedure);
                await _context.SaveChangesAsync();

                // Commit transaction
                await tx.CommitAsync();

                // Success
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "Create",
                    customMessage: "Error creating patient",
                    additionalData: new { PatientName = model.PatientName, PatientCNIC = model.PatientCNIC },
                    serviceName: "PatientsController"
                );

                ViewBag.ProcedureStatuses = new List<string> { "Scheduled", "In-Progress", "Completed", "Cancelled" };
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                return View(model); // Stay on form page with error
            }
        }


        [HttpGet]
        public JsonResult GetCitiesByProvince(string province)
        {
            var cities = new Dictionary<string, List<string>>
            {
                ["Punjab"] = new List<string> {
            "Lahore", "Faisalabad", "Rawalpindi", "Multan", "Gujranwala", "Sialkot",
            "Sargodha", "Bahawalpur", "Jhang", "Sheikhupura", "Gujrat", "Kasur",
            "Sahiwal", "Okara", "Wah Cantonment", "Dera Ghazi Khan", "Burewala",
            "Chenab Nagar", "Hafizabad", "Muzaffargarh", "Khanpur", "Gojra",
            "Bahawalnagar", "Muridke", "Pak Pattan", "Taxila", "Vehari", "Attock",
            "Mandi Bahauddin", "Chiniot", "Kamalia", "Jhelum", "Chakwal", "Khanewal",
            "Narowal", "Mianwali", "Bhakkar", "Layyah", "Rajanpur", "Lodhran"
        },
                ["Sindh"] = new List<string> {
            "Karachi", "Hyderabad", "Sukkur", "Larkana", "Nawabshah", "Mirpurkhas",
            "Jacobabad", "Shikarpur", "Khairpur", "Dadu", "Ghotki", "Sanghar",
            "Badin", "Tando Allahyar", "Tando Muhammad Khan", "Thatta", "Matiari",
            "Umerkot", "Tharparkar", "Naushahro Feroze", "Kashmore", "Qambar Shahdadkot",
            "Jamshoro", "Kotri", "Mehar", "Ratodero", "Kandhkot", "Daharki",
            "Sakrand", "Sehwan", "Moro", "Benazirabad", "Mithi", "Shahdadkot"
        },
                ["KPK"] = new List<string> {
            "Peshawar", "Mardan", "Mingora", "Kohat", "Dera Ismail Khan", "Bannu",
            "Swabi", "Charsadda", "Nowshera", "Mansehra", "Abbottabad", "Karak",
            "Hangu", "Haripur", "Parachinar", "Lakki Marwat", "Tank", "Chitral",
            "Dir", "Swat", "Kurram", "Orakzai", "Khyber", "Mohmand", "Bajaur",
            "North Waziristan", "South Waziristan", "Buner", "Shangla", "Upper Dir",
            "Lower Dir", "Upper Chitral", "Lower Chitral"
        },
                ["Balochistan"] = new List<string> {
            "Quetta", "Turbat", "Khuzdar", "Hub", "Chaman", "Gwadar", "Zhob",
            "Sibi", "Loralai", "Pishin", "Killa Abdullah", "Mastung", "Kalat",
            "Lasbela", "Nasirabad", "Jafarabad", "Jhal Magsi", "Killa Saifullah",
            "Musakhel", "Ziarat", "Awaran", "Barkhan", "Duki", "Harnai", "Kohlu",
            "Dera Bugti", "Panjgur", "Washuk", "Kech", "Nushki", "Chagai", "Kharan"
        },
                ["Islamabad"] = new List<string> { "Islamabad" },
                ["AJK"] = new List<string> {
            "Muzaffarabad", "Mirpur", "Rawalakot", "Kotli", "Bhimber", "Bagh",
            "Sudhnoti", "Poonch", "Haveli", "Neelum"
        },
                ["Gilgit"] = new List<string> {
            "Gilgit", "Skardu", "Hunza", "Nagar", "Ghizer", "Shigar", "Kharmang",
            "Ganche", "Roundu", "Astore", "Diamer", "Ghanche"
        }
            };

            if (cities.ContainsKey(province))
            {
                return Json(cities[province].OrderBy(c => c).ToList());
            }

            return Json(new List<string>());
        }


        // GET: Patients/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .Include(p => p.EmergencyContacts.Where(ec => ec.IsActive))
                .Include(p => p.ReferralInfos.Where(ri => ri.IsActive))
                .Include(p => p.Procedures.Where(proc => proc.IsActive)) // ADD THIS LINE
                .Include(p => p.MedicalRecords.Where(mr => mr.IsActive))

                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            // Get clinical reports for this patient
            var clinicalReports = await _context.ClinicalReports
                .Where(cr => cr.PatientId == id && cr.IsActive)
                .ToListAsync();

            var medicalRecord = patient.MedicalRecords.FirstOrDefault();
            // GET LATEST PROCEDURE FOR THIS PATIENT (ADD THIS)
            var latestProcedure = patient.Procedures
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            // SET VIEWBAG FOR PROCEDURE (ADD THIS)
            if (latestProcedure != null)
            {
                ViewBag.ProcedureId = latestProcedure.ProcedureId;
                ViewBag.ProcedureDate = latestProcedure.ProcedureDate;
                ViewBag.ProcedureDuration = latestProcedure.ProcedureDuration;
                ViewBag.ProcedureROI = latestProcedure.ProcedureROI;
                ViewBag.ProcedureStatus = latestProcedure.ProcedureStatus;
                ViewBag.ProcedureRemarks = latestProcedure.ProcedureRemarks;
            }

            //   var boardApproval = medicalRecord?.BoardApproval;

            var viewModel = new PatientEditViewModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.PatientName,
                PatientCNIC = patient.PatientCNIC,
                PatientDOB = patient.PatientDOB,
                PatientMRNumber = patient.PatientMRNumber,
                PatientPhone = patient.PatientPhone,
                PatientGender = patient.PatientGender,
                PatientAge = patient.PatientAge,
                PatientAddress = patient.PatientAddress,
                PatientCity = patient.PatientCity,
                PatientProvince = patient.PatientProvince,
                PatientInsuranceInfo = patient.PatientInsuranceInfo,
                PatientPaymentMode = patient.PatientPaymentMode,

                // Emergency Contacts
                EmergencyContacts = patient.EmergencyContacts.Select(ec => new EmergencyContactEditViewModel
                {
                    EmergencyContactId = ec.EmergencyContactId,
                    Name = ec.EmergencyContactName,
                    CNIC = ec.EmergencyContactCNIC,
                    Relationship = ec.EmergencyContactRelationship,
                    Phone = ec.EmergencyContactPhone,
                    Address = ec.EmergencyContactAddress,
                    City = ec.EmergencyContactCity,
                    Province = ec.EmergencyContactProvince
                }).ToList(),

                // Referral Infos
                ReferralInfos = patient.ReferralInfos.Select(ri => new ReferralInfoEditViewModel
                {
                    ReferralInfoId = ri.ReferralInfoId,
                    HospitalName = ri.ReferralHospitalName,
                    City = ri.ReferralCity,
                    DoctorName = ri.ReferralDoctorName,
                    Department = ri.ReferralDepartment,
                    Phone = ri.ReferralPhone,
                    Email = ri.ReferralEmail
                }).ToList(),

                // Medical Record
                MedicalRecordId = medicalRecord?.MedicalRecordId,
                MedicalRecordCancerType = !string.IsNullOrEmpty(medicalRecord?.MedicalRecordCancerType)
                    ? medicalRecord.MedicalRecordCancerType.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    : new List<string>(),
                MedicalRecordCancerStage = medicalRecord?.MedicalRecordCancerStage,
                MedicalRecordComorbidities = medicalRecord?.MedicalRecordComorbidities,
                MedicalRecordTumorLocation = medicalRecord?.MedicalRecordTumorLocation,
                MedicalRecordAllergies = medicalRecord?.MedicalRecordAllergies,
                MedicalRecordClinicalHistory = medicalRecord?.MedicalRecordClinicalHistory,

                // Clinical Reports
                ClinicalReports = clinicalReports.Select(cr => new ClinicalReportEditViewModel
                {
                    ClinicalReportId = cr.ClinicalReportId,
                    ReportType = cr.ReportType,
                    ExistingFilePath = cr.ReportFilePath
                }).ToList(),

                // Board Approval

            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientProcedureSummary(Guid id)
        {
            var procedures = await _context.Procedures
                .Where(p => p.PatientId == id && p.IsActive)
                .OrderByDescending(p => p.ProcedureDate)
                .Take(5)
                .Select(p => new
                {
                    p.ProcedureId,
                    p.ProcedureDate,
                    p.ProcedureROI,
                    p.ProcedureStatus
                })
                .ToListAsync();

            var summary = new
            {
                totalProcedures = procedures.Count,
                scheduledProcedures = procedures.Count(p => p.ProcedureStatus == "Scheduled"),
                completedProcedures = procedures.Count(p => p.ProcedureStatus == "Completed"),
                inProgressProcedures = procedures.Count(p => p.ProcedureStatus == "In-Progress"),
                recentProcedures = procedures
            };

            return Json(summary);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PatientEditViewModel model)
        {
            if (id != model.PatientId)
                return NotFound();

            // Remove validation for optional fields
            ModelState.Remove("BoardApprovalAttachment");

            // Remove validation for Emergency Contacts optional fields
            for (int i = 0; i < model.EmergencyContacts.Count; i++)
            {
                ModelState.Remove($"EmergencyContacts[{i}].CNIC");
                ModelState.Remove($"EmergencyContacts[{i}].Phone");
                ModelState.Remove($"EmergencyContacts[{i}].Address");
                ModelState.Remove($"EmergencyContacts[{i}].Province");
                ModelState.Remove($"EmergencyContacts[{i}].City");
            }

            // Remove validation for Referral Infos optional fields
            for (int i = 0; i < model.ReferralInfos.Count; i++)
            {
                ModelState.Remove($"ReferralInfos[{i}].Department");
                ModelState.Remove($"ReferralInfos[{i}].Phone");
                ModelState.Remove($"ReferralInfos[{i}].Email");
            }

            // Remove validation for Clinical Reports file (optional for existing)
            for (int i = 0; i < model.ClinicalReports.Count; i++)
            {
                ModelState.Remove($"ClinicalReports[{i}].ReportFile");
            }

            if (!ModelState.IsValid)
            {
                // Log errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Patient edit validation failed",
                    recordId: model.PatientId.ToString(),
                    tableName: "Patients",
                    additionalData: new { PatientId = model.PatientId, Errors = errors },
                    serviceName: "PatientsController",
                    methodName: "Edit"
                );

                TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // Log user not found
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "User not found during patient edit",
                    recordId: model.PatientId.ToString(),
                    tableName: "Patients",
                    additionalData: new { PatientId = model.PatientId },
                    serviceName: "PatientsController",
                    methodName: "Edit"
                );

                ModelState.AddModelError("", "User not found, please login again.");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update Patient Basic Info
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                    return NotFound();

                patient.PatientName = model.PatientName;
                patient.PatientCNIC = model.PatientCNIC;
                patient.PatientDOB = model.PatientDOB;
                patient.PatientMRNumber = model.PatientMRNumber;
                patient.PatientPhone = model.PatientPhone;
                patient.PatientGender = model.PatientGender;
                patient.PatientAge = model.PatientAge;
                patient.PatientAddress = model.PatientAddress;
                patient.PatientCity = model.PatientCity;
                patient.PatientProvince = model.PatientProvince;
                patient.PatientInsuranceInfo = model.PatientInsuranceInfo;
                patient.PatientPaymentMode = model.PatientPaymentMode;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                // 2. Handle Emergency Contacts
                await HandleEmergencyContactsUpdate(model, userId);

                // 3. Handle Referral Infos
                await HandleReferralInfosUpdate(model, userId);

                // 4. Handle Medical Record
                await HandleMedicalRecordUpdate(model, userId);

                // 5. Handle Clinical Reports (with file uploads)
                await HandleClinicalReportsUpdate(model, userId);



                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Patient updated successfully!";
                return RedirectToAction(nameof(Details), new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "Edit",
                    customMessage: "Error updating patient",
                    additionalData: new { PatientId = model.PatientId },
                    serviceName: "PatientsController"
                );

                TempData["ErrorMessage"] = $"Error updating patient: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit2(Guid id, PatientEditViewModel model)
        {
            if (id != model.PatientId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                // Log validation failure
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "Patient edit validation failed (Edit2)",
                    recordId: id.ToString(),
                    tableName: "Patients",
                    additionalData: new { PatientId = id, Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() },
                    serviceName: "PatientsController",
                    methodName: "Edit2"
                );

                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // Log user not found
                await _auditLogService.LogAsync(
                    actionType: "VALIDATION_FAILED",
                    message: "User not found during patient edit (Edit2)",
                    recordId: id.ToString(),
                    tableName: "Patients",
                    additionalData: new { PatientId = id },
                    serviceName: "PatientsController",
                    methodName: "Edit2"
                );

                ModelState.AddModelError("", "User not found, please login again.");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update Patient Basic Info
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                    return NotFound();

                patient.PatientName = model.PatientName;
                patient.PatientCNIC = model.PatientCNIC;
                patient.PatientDOB = model.PatientDOB;
                patient.PatientMRNumber = model.PatientMRNumber;
                patient.PatientPhone = model.PatientPhone;
                patient.PatientGender = model.PatientGender;
                patient.PatientAge = model.PatientAge;
                patient.PatientAddress = model.PatientAddress;
                patient.PatientCity = model.PatientCity;
                patient.PatientProvince = model.PatientProvince;
                patient.PatientInsuranceInfo = model.PatientInsuranceInfo;
                patient.PatientPaymentMode = model.PatientPaymentMode;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                // 2. Handle Emergency Contacts
                await HandleEmergencyContactsUpdate(model, userId);

                // 3. Handle Referral Infos
                await HandleReferralInfosUpdate(model, userId);

                // 4. Handle Medical Record
                await HandleMedicalRecordUpdate(model, userId);

                // 5. Handle Clinical Reports (with file uploads)
                await HandleClinicalReportsUpdate(model, userId);



                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Patient updated successfully!";
                return RedirectToAction(nameof(Details), new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                // Log exception
                await _auditLogService.LogExceptionAsync(
                    exception: ex,
                    methodName: "Edit2",
                    customMessage: "Error updating patient (Edit2)",
                    additionalData: new { PatientId = id },
                    serviceName: "PatientsController"
                );

                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error updating patient: {ex.Message}");
                return View(model);
            }
        }

        // Helper Methods
        private async Task HandleEmergencyContactsUpdate(PatientEditViewModel model, string userId)
        {
            // Get all existing emergency contacts for this patient
            var existingContacts = await _context.EmergencyContacts
                .Where(ec => ec.PatientId == model.PatientId && ec.IsActive)
                .ToListAsync();

            // Get IDs from the model
            var modelContactIds = model.EmergencyContacts
                .Where(ec => ec.EmergencyContactId.HasValue)
                .Select(ec => ec.EmergencyContactId.Value)
                .ToList();

            // 1. Mark deleted contacts (those marked as deleted OR not in model anymore)
            foreach (var existingContact in existingContacts)
            {
                var modelContact = model.EmergencyContacts
                    .FirstOrDefault(ec => ec.EmergencyContactId == existingContact.EmergencyContactId);

                if (modelContact == null || modelContact.IsDeleted)
                {
                    existingContact.IsActive = false; // Soft delete
                    _context.EmergencyContacts.Update(existingContact);
                }
                else
                {
                    // 2. Update existing contacts
                    existingContact.EmergencyContactName = modelContact.Name;
                    existingContact.EmergencyContactCNIC = modelContact.CNIC;
                    existingContact.EmergencyContactRelationship = modelContact.Relationship;
                    existingContact.EmergencyContactPhone = modelContact.Phone;
                    existingContact.EmergencyContactAddress = modelContact.Address;
                    existingContact.EmergencyContactCity = modelContact.City;
                    existingContact.EmergencyContactProvince = modelContact.Province;
                    _context.EmergencyContacts.Update(existingContact);
                }
            }

            // 3. Add new contacts (EmergencyContactId is null)
            var newContacts = model.EmergencyContacts
                .Where(ec => !ec.EmergencyContactId.HasValue && !ec.IsDeleted && !string.IsNullOrWhiteSpace(ec.Name))
                .ToList();

            foreach (var newContact in newContacts)
            {
                var ec = new EmergencyContact
                {
                    EmergencyContactName = newContact.Name,
                    EmergencyContactCNIC = newContact.CNIC,
                    EmergencyContactRelationship = newContact.Relationship,
                    EmergencyContactPhone = newContact.Phone,
                    EmergencyContactAddress = newContact.Address,
                    EmergencyContactCity = newContact.City,
                    EmergencyContactProvince = newContact.Province,
                    PatientId = model.PatientId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.EmergencyContacts.Add(ec);
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandleReferralInfosUpdate(PatientEditViewModel model, string userId)
        {
            var existingReferrals = await _context.ReferralInfos
                .Where(ri => ri.PatientId == model.PatientId && ri.IsActive)
                .ToListAsync();

            // Mark deleted
            foreach (var existingReferral in existingReferrals)
            {
                var modelReferral = model.ReferralInfos
                    .FirstOrDefault(ri => ri.ReferralInfoId == existingReferral.ReferralInfoId);

                if (modelReferral == null || modelReferral.IsDeleted)
                {
                    existingReferral.IsActive = false;
                    _context.ReferralInfos.Update(existingReferral);
                }
                else
                {
                    // Update existing
                    existingReferral.ReferralHospitalName = modelReferral.HospitalName;
                    existingReferral.ReferralCity = modelReferral.City;
                    existingReferral.ReferralDoctorName = modelReferral.DoctorName;
                    existingReferral.ReferralDepartment = modelReferral.Department;
                    existingReferral.ReferralPhone = modelReferral.Phone;
                    existingReferral.ReferralEmail = modelReferral.Email;
                    _context.ReferralInfos.Update(existingReferral);
                }
            }

            // Add new
            var newReferrals = model.ReferralInfos
                .Where(ri => !ri.ReferralInfoId.HasValue && !ri.IsDeleted && !string.IsNullOrWhiteSpace(ri.HospitalName))
                .ToList();

            foreach (var newReferral in newReferrals)
            {
                var refInfo = new ReferralInfo
                {
                    ReferralHospitalName = newReferral.HospitalName,
                    ReferralCity = newReferral.City,
                    ReferralDoctorName = newReferral.DoctorName,
                    ReferralDepartment = newReferral.Department,
                    ReferralPhone = newReferral.Phone,
                    ReferralEmail = newReferral.Email,
                    PatientId = model.PatientId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.ReferralInfos.Add(refInfo);
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandleMedicalRecordUpdate(PatientEditViewModel model, string userId)
        {
            MedicalRecord medicalRecord;

            if (model.MedicalRecordId.HasValue)
            {
                // Update existing
                medicalRecord = await _context.MedicalRecords.FindAsync(model.MedicalRecordId.Value);
                if (medicalRecord == null) return;
            }
            else
            {
                // Create new
                medicalRecord = new MedicalRecord
                {
                    PatientId = model.PatientId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.MedicalRecords.Add(medicalRecord);
            }

            // Update fields
            medicalRecord.MedicalRecordCancerType = model.MedicalRecordCancerType != null && model.MedicalRecordCancerType.Any()
                ? string.Join(",", model.MedicalRecordCancerType)
                : null;
            medicalRecord.MedicalRecordCancerStage = model.MedicalRecordCancerStage;
            medicalRecord.MedicalRecordComorbidities = model.MedicalRecordComorbidities;
            medicalRecord.MedicalRecordTumorLocation = model.MedicalRecordTumorLocation;
            medicalRecord.MedicalRecordAllergies = model.MedicalRecordAllergies;
            medicalRecord.MedicalRecordClinicalHistory = model.MedicalRecordClinicalHistory;

            await _context.SaveChangesAsync();

            // Update model's MedicalRecordId for board approval handling
            model.MedicalRecordId = medicalRecord.MedicalRecordId;
        }

        private async Task HandleClinicalReportsUpdate(PatientEditViewModel model, string userId)
        {
            var existingReports = await _context.ClinicalReports
                .Where(cr => cr.PatientId == model.PatientId && cr.IsActive)
                .ToListAsync();

            // Mark deleted
            foreach (var existingReport in existingReports)
            {
                var modelReport = model.ClinicalReports
                    .FirstOrDefault(cr => cr.ClinicalReportId == existingReport.ClinicalReportId);

                if (modelReport == null || modelReport.IsDeleted)
                {
                    existingReport.IsActive = false;
                    _context.ClinicalReports.Update(existingReport);

                    // Optionally delete physical file
                    if (!string.IsNullOrEmpty(existingReport.ReportFilePath))
                    {
                        var physicalPath = Path.Combine(_env.WebRootPath, existingReport.ReportFilePath.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                }
                else if (modelReport.ReportFile != null && modelReport.ReportFile.Length > 0)
                {
                    // Update: delete old file and upload new
                    if (!string.IsNullOrEmpty(existingReport.ReportFilePath))
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, existingReport.ReportFilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Upload new file
                    var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "clinicalreports");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(modelReport.ReportFile.FileName);
                    var filePath = Path.Combine(uploadFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await modelReport.ReportFile.CopyToAsync(stream);
                    }
                    existingReport.ReportFilePath = "/uploads/clinicalreports/" + fileName;
                    existingReport.ReportType = modelReport.ReportType;
                    _context.ClinicalReports.Update(existingReport);
                }
            }

            // Add new reports
            var newReports = model.ClinicalReports
                .Where(cr => !cr.ClinicalReportId.HasValue && !cr.IsDeleted && cr.ReportFile != null && cr.ReportFile.Length > 0)
                .ToList();

            foreach (var newReport in newReports)
            {
                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "clinicalreports");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var fileName = Guid.NewGuid() + Path.GetExtension(newReport.ReportFile.FileName);
                var filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newReport.ReportFile.CopyToAsync(stream);
                }

                var clinicalReport = new ClinicalReport
                {
                    PatientId = model.PatientId,
                    ReportType = newReport.ReportType,
                    ReportFilePath = "/uploads/clinicalreports/" + fileName,
                    ReportDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.ClinicalReports.Add(clinicalReport);
            }

            await _context.SaveChangesAsync();
        }



        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(Guid id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}
