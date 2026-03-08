using CryoablationSoftware.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CryoablationSoftware.ViewModel
{
    public class PatientCreateViewModel
    {
        // Patient Fields

        [Display(Name = "Patient Name")]
        public string PatientName { get; set; }

        [Display(Name = "Patient CNIC")]
        public string PatientCNIC { get; set; }

        [Display(Name = "Date of Birth")]
        public DateTime? PatientDOB { get; set; }

        [Display(Name = "MR Number")]
        public string PatientMRNumber { get; set; }

        [Display(Name = "Phone Number")]
        public string PatientPhone { get; set; }

        [Display(Name = "Gender")]
        public string PatientGender { get; set; }

        public string ProcedureStatus { get; set; } = "Scheduled";


        [Display(Name = "Age")]
        public int PatientAge { get; set; }

        [Display(Name = "Address")]
        public string PatientAddress { get; set; }

        [Display(Name = "City")]
        public string PatientCity { get; set; }

        [Display(Name = "Province")]
        public string PatientProvince { get; set; }

        [Display(Name = "Insurance Information")]
        public string PatientInsuranceInfo { get; set; }

        [Display(Name = "Payment Mode")]
        public string PatientPaymentMode { get; set; }

        // Emergency Contacts (List)
        public List<EmergencyContactViewModel> EmergencyContacts { get; set; } = new();

        // Referral Infos (List)
        public List<ReferralInfoViewModel> ReferralInfos { get; set; } = new();

        // Medical Record Fields
        [Display(Name = "Cancer Types")]
        public List<string> MedicalRecordCancerType { get; set; } = new();

        [Display(Name = "Cancer Stage")]
        public string MedicalRecordCancerStage { get; set; }

        [Display(Name = "Comorbidities")]
        public string MedicalRecordComorbidities { get; set; }

        [Display(Name = "Tumor Location")]
        public string? MedicalRecordTumorLocation { get; set; }

        [Display(Name = "Tumor Size")]
        public string MedicalRecordAllergies { get; set; }

        [Display(Name = "Clinical History")]
        public string MedicalRecordClinicalHistory { get; set; }


        // Clinical Reports
        public List<ClinicalReportViewModel> ClinicalReports { get; set; } = new();

        // Board Approval
        //[Display(Name = "Board Approval Date")]
        //public DateTime? BoardApprovalDate { get; set; }

        //[Display(Name = "Board Approval Decision")]
        //public bool? BoardApprovalDecision { get; set; }

        //[Display(Name = "Board Approval Letter Number")]
        //public string BoardApprovalLetterNumber { get; set; }

        //[Display(Name = "Board Approval Attachment")]
        //public IFormFile BoardApprovalAttachment { get; set; }

        // Procedure
        [Display(Name = "Procedure Date")]
        public DateTime? ProcedureDate { get; set; }

        [Display(Name = "Procedure Duration")]
        public string ProcedureDuration { get; set; }

        [Display(Name = "Procedure ROI")]
        public string ProcedureROI { get; set; }

        [Display(Name = "Procedure Remarks")]
        public string ProcedureRemarks { get; set; }

        // NEW FIELDS - Doctor & Engineer Names
        [Required(ErrorMessage = "Primary Doctor name is required")]
        [StringLength(200, ErrorMessage = "Primary Doctor name cannot exceed 200 characters")]
        [Display(Name = "Primary Doctor")]
        public string PrimaryDoctorName { get; set; }

        [StringLength(200, ErrorMessage = "Consultant Doctor 1 name cannot exceed 200 characters")]
        [Display(Name = "Consultant Doctor 1 (Optional)")]
        public string? ConsultantDoctor1Name { get; set; }

        [StringLength(200, ErrorMessage = "Consultant Doctor 2 name cannot exceed 200 characters")]
        [Display(Name = "Consultant Doctor 2 (Optional)")]
        public string? ConsultantDoctor2Name { get; set; }

        [Required(ErrorMessage = "Operating Engineer name is required")]
        [StringLength(200, ErrorMessage = "Operating Engineer name cannot exceed 200 characters")]
        [Display(Name = "Operating Engineer")]
        public string OperatingEngineerName { get; set; }


    }

    public class EmergencyContactViewModel
    {
        public string Name { get; set; }
        public string CNIC { get; set; }
        public string Relationship { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
    }

    public class ReferralInfoViewModel
    {
        [Display(Name = "Hospital Name")]
        public string HospitalName { get; set; }
        public string? City { get; set; }
        [Display(Name = "Doctor Name")]
        public string? DoctorName { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class ClinicalReportViewModel
    {
        [Display(Name = "Report Type")]
        public string ReportType { get; set; }
        [Display(Name = "Report File")]
        public IFormFile ReportFile { get; set; }
    }
}
