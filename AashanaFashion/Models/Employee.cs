using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    public enum SalaryType
    {
        [Display(Name = "Monthly Fixed Salary")]
        MonthlySalary,

        [Display(Name = "Daily Wage")]
        DailyWage,

        [Display(Name = "Hourly Rate")]
        HourlyRate
    }

    [Table("Employees")]
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; } = DateTime.Today;

        [Display(Name = "Salary Type")]
        public SalaryType SalaryType { get; set; } = SalaryType.DailyWage;

        [Display(Name = "Base Rate / Salary (₹)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRate { get; set; }

        [Display(Name = "Standard Daily Hours")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StandardDailyHours { get; set; } = 8.0m;

        [Display(Name = "Overtime Hourly Rate (₹)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeHourlyRate { get; set; } = 0m;

        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Bank Account No")]
        public string? BankAccountNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "IFSC Code")]
        public string? BankIFSC { get; set; }

        [StringLength(100)]
        [Display(Name = "UPI ID / PhonePe / GPay")]
        public string? UpiId { get; set; }

        // Biometric Face Data
        public string? FaceDescriptor { get; set; } // 128-float vector stored as JSON array string: "[0.123,-0.045,...]"

        [StringLength(300)]
        [Display(Name = "Face Photo")]
        public string? FacePhotoPath { get; set; } // Path to stored face image e.g. /uploads/faces/emp_1.jpg

        [Display(Name = "Face Registered")]
        public bool IsFaceRegistered { get; set; } = false;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<SalaryRecord> SalaryRecords { get; set; } = new List<SalaryRecord>();
    }
}
