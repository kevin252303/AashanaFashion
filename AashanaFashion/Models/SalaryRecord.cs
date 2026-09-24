using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    public enum PayrollStatus
    {
        Pending,
        Approved,
        Paid
    }

    [Table("SalaryRecords")]
    public class SalaryRecord
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        [Display(Name = "Total Working Days")]
        public int TotalWorkingDays { get; set; } = 26;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Days Present")]
        public decimal DaysPresent { get; set; } = 0m;

        [Display(Name = "Half Days")]
        public int HalfDays { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Days Absent")]
        public decimal DaysAbsent { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Hours Worked")]
        public decimal TotalHoursWorked { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Overtime Hours")]
        public decimal TotalOvertimeHours { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Salary Earned (₹)")]
        public decimal BaseSalaryEarned { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Overtime Pay (₹)")]
        public decimal OvertimePay { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Bonus / Allowance (₹)")]
        public decimal BonusAllowance { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Deductions / Advance (₹)")]
        public decimal Deductions { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Net Payable Salary (₹)")]
        public decimal NetSalary { get; set; } = 0m;

        [Display(Name = "Payment Status")]
        public PayrollStatus PaymentStatus { get; set; } = PayrollStatus.Pending;

        [Display(Name = "Paid Date")]
        public DateTime? PaidDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; } // Cash, Bank Transfer, UPI, Cheque

        [StringLength(100)]
        [Display(Name = "Payment Ref / UTR")]
        public string? PaymentReference { get; set; }

        [StringLength(300)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
