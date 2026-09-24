using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    public enum AttendanceStatus
    {
        Present,
        Late,
        HalfDay,
        Absent,
        OnLeave,
        Holiday
    }

    public enum VerificationMethod
    {
        FaceScan,
        ManualPunch,
        Kiosk,
        BiometricDevice
    }

    [Table("AttendanceRecords")]
    public class AttendanceRecord
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        public DateTime CheckInTime { get; set; } = DateTime.Now;

        public DateTime? CheckOutTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; } = 0m;

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [Display(Name = "Verification Method")]
        public VerificationMethod VerificationMethod { get; set; } = VerificationMethod.FaceScan;

        [Display(Name = "Face Confidence (%)")]
        public double? FaceConfidence { get; set; }

        [StringLength(300)]
        public string? CheckInPhoto { get; set; }

        [StringLength(300)]
        public string? CheckOutPhoto { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public int? DeviceId { get; set; }

        [ForeignKey("DeviceId")]
        public BiometricDevice? Device { get; set; }

        [StringLength(100)]
        public string? DeviceLogId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
