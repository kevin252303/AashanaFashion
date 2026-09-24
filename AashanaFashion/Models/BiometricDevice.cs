using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    [Table("BiometricDevices")]
    public class BiometricDevice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Device Name")]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Serial Number / Device Identifier")]
        public string DeviceIdentifier { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "Installation Location")]
        public string? Location { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "API Key / Auth Token")]
        public string ApiKey { get; set; } = Guid.NewGuid().ToString("N");

        [StringLength(50)]
        [Display(Name = "Device Brand / Model")]
        public string? DeviceModel { get; set; } = "Universal Face Scanner";

        [Display(Name = "Last Heartbeat / Sync")]
        public DateTime? LastHeartbeat { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }

    public class BiometricPushDto
    {
        [Required]
        public string EmployeeCode { get; set; } = string.Empty;

        public DateTime PunchTime { get; set; } = DateTime.Now;

        public string? PunchType { get; set; } = "Auto"; // CheckIn, CheckOut, Auto

        public string? DeviceIdentifier { get; set; }

        public string? ApiKey { get; set; }

        public string? VerificationType { get; set; } = "Face"; // Face, Finger, Card, Password

        public string? LogId { get; set; }

        public double? ConfidenceScore { get; set; }
    }
}
