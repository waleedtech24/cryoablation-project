using System.ComponentModel.DataAnnotations;
using static CryoablationSoftware.ViewModel.ProcedureEditViewModel;

namespace CryoablationSoftware.ViewModel
{
    public class PatientEditViewModel
    {
        public Guid PatientId { get; set; }

        // Patient Basic Info
        [Required]
        public string PatientName { get; set; }

        public string PatientCNIC { get; set; }
        public DateTime? PatientDOB { get; set; }
        public string PatientMRNumber { get; set; }
        public string PatientPhone { get; set; }
        public string PatientGender { get; set; }

        [Required]
        public int PatientAge { get; set; }

        public string PatientAddress { get; set; }
        public string PatientCity { get; set; }
        public string PatientProvince { get; set; }
        public string PatientInsuranceInfo { get; set; }
        public string PatientPaymentMode { get; set; }

        // Emergency Contacts - NO REQUIRED
        public List<EmergencyContactEditViewModel> EmergencyContacts { get; set; } = new();

        // Referral Infos - NO REQUIRED
        public List<ReferralInfoEditViewModel> ReferralInfos { get; set; } = new();

        // Medical Record Fields
        public Guid? MedicalRecordId { get; set; }
        public List<string> MedicalRecordCancerType { get; set; } = new();
        public string MedicalRecordCancerStage { get; set; }
        public string MedicalRecordComorbidities { get; set; }
        public string? MedicalRecordTumorLocation { get; set; }
        public string MedicalRecordAllergies { get; set; }
        public string MedicalRecordClinicalHistory { get; set; }

        // Clinical Reports - NO REQUIRED
        public List<ClinicalReportEditViewModel> ClinicalReports { get; set; } = new();


    }

    public class EmergencyContactEditViewModel
    {
        public Guid? EmergencyContactId { get; set; }

        // NO REQUIRED HERE
        public string Name { get; set; }
        public string? CNIC { get; set; }
        public string? Relationship { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    public class ReferralInfoEditViewModel
    {
        public Guid? ReferralInfoId { get; set; }

        // NO REQUIRED HERE
        public string HospitalName { get; set; }
        public string? City { get; set; }
        public string? DoctorName { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    public class ClinicalReportEditViewModel
    {
        public Guid? ClinicalReportId { get; set; }

        // NO REQUIRED HERE
        public string ReportType { get; set; }
        public IFormFile? ReportFile { get; set; }
        public string ExistingFilePath { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

}
