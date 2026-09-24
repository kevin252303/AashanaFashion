using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models
{
    public class FaceEnrollmentDto
    {
        public int EmployeeId { get; set; }
        public string FaceDescriptor { get; set; } = string.Empty;
        public string? FaceImageBase64 { get; set; }
    }

    public class RecordPunchDto
    {
        public int EmployeeId { get; set; }
        public string VerificationMethod { get; set; } = "FaceScan";
        public double? FaceConfidence { get; set; }
        public string? SnapshotBase64 { get; set; }
    }

    public class EmployeeFaceProfileDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? FacePhotoPath { get; set; }
        public string? FaceDescriptor { get; set; }
        public bool IsCheckedInToday { get; set; }
        public string? TodayCheckInTime { get; set; }
        public string? TodayCheckOutTime { get; set; }
    }

    public class ManualPunchViewModel
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan CheckInTime { get; set; } = new TimeSpan(9, 0, 0);

        [DataType(DataType.Time)]
        public TimeSpan? CheckOutTime { get; set; } = new TimeSpan(18, 0, 0);

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [StringLength(300)]
        public string? Notes { get; set; }
    }

    public class PayrollSummaryViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public decimal TotalGrossSalary { get; set; }
        public decimal TotalOvertimePay { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNetPayable { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public List<SalaryRecord> Records { get; set; } = new();
    }
}
