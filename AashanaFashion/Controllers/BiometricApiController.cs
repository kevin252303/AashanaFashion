using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AashanaFashion.Controllers
{
    [ApiController]
    public class BiometricApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BiometricApiController> _logger;

        public BiometricApiController(AppDbContext context, ILogger<BiometricApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================================
        // 1. UNIVERSAL CLOUD PUSH / WEBHOOK API (JSON & Form Data)
        // Endpoint: POST /api/biometric/push
        // =========================================================================
        [HttpPost("api/biometric/push")]
        public async Task<IActionResult> UniversalPush([FromBody] BiometricPushDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.EmployeeCode))
            {
                return BadRequest(new { success = false, message = "EmployeeCode is required." });
            }

            // Verify or lookup device
            BiometricDevice? device = null;
            if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            {
                device = await _context.BiometricDevices.FirstOrDefaultAsync(d => d.ApiKey == dto.ApiKey);
            }
            else if (!string.IsNullOrWhiteSpace(dto.DeviceIdentifier))
            {
                device = await _context.BiometricDevices.FirstOrDefaultAsync(d => d.DeviceIdentifier == dto.DeviceIdentifier);
            }

            if (device != null)
            {
                device.LastHeartbeat = DateTime.Now;
            }

            var punchTime = dto.PunchTime == default ? DateTime.Now : dto.PunchTime;
            var punchDate = punchTime.Date;

            // Resolve employee (support "EMP-001" or raw numeric "1" / "001")
            var empCode = dto.EmployeeCode.Trim();
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == empCode || 
                                          e.EmployeeCode == $"EMP-{empCode}" ||
                                          e.EmployeeCode.EndsWith($"-{empCode.PadLeft(3, '0')}"));

            if (employee == null)
            {
                return NotFound(new { success = false, message = $"Employee '{empCode}' not found in Aashana Fashion." });
            }

            if (!employee.IsActive)
            {
                return BadRequest(new { success = false, message = $"Employee '{employee.FullName}' is inactive." });
            }

            // Check duplicate log ID if provided by device
            if (!string.IsNullOrWhiteSpace(dto.LogId))
            {
                var existingLog = await _context.AttendanceRecords
                    .AnyAsync(a => a.DeviceLogId == dto.LogId && a.DeviceId == (device != null ? device.Id : null));
                if (existingLog)
                {
                    return Ok(new { success = true, duplicate = true, message = "Log already processed." });
                }
            }

            // Check today's attendance record
            var attendance = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == punchDate);

            string actionType;

            if (attendance == null)
            {
                // PUNCH IN
                attendance = new AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    Date = punchDate,
                    CheckInTime = punchTime,
                    CheckOutTime = null,
                    VerificationMethod = VerificationMethod.BiometricDevice,
                    DeviceId = device?.Id,
                    DeviceLogId = dto.LogId,
                    FaceConfidence = dto.ConfidenceScore ?? 99.0,
                    Notes = $"Biometric punch from {device?.DeviceName ?? "External Terminal"} ({dto.VerificationType ?? "Face"})"
                };

                // Late check-in after 9:30 AM
                if (punchTime.TimeOfDay > new TimeSpan(9, 30, 0))
                {
                    attendance.Status = AttendanceStatus.Late;
                }
                else
                {
                    attendance.Status = AttendanceStatus.Present;
                }

                _context.AttendanceRecords.Add(attendance);
                actionType = "CheckIn";
            }
            else
            {
                // PUNCH OUT (or update check-out to latest punch)
                if (punchTime > attendance.CheckInTime)
                {
                    attendance.CheckOutTime = punchTime;
                    var duration = punchTime - attendance.CheckInTime;
                    attendance.TotalHours = Math.Round((decimal)duration.TotalHours, 2);

                    var stdHours = employee.StandardDailyHours > 0 ? employee.StandardDailyHours : 8.0m;
                    if (attendance.TotalHours > stdHours)
                    {
                        attendance.OvertimeHours = Math.Round(attendance.TotalHours - stdHours, 2);
                    }
                    else
                    {
                        attendance.OvertimeHours = 0;
                    }

                    if (attendance.TotalHours < 4.0m && attendance.Status != AttendanceStatus.Late)
                    {
                        attendance.Status = AttendanceStatus.HalfDay;
                    }
                    else if (attendance.Status == AttendanceStatus.HalfDay && attendance.TotalHours >= 4.0m)
                    {
                        attendance.Status = AttendanceStatus.Present;
                    }

                    actionType = "CheckOut";
                }
                else
                {
                    actionType = "IgnoredEarlierPunch";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                action = actionType,
                employee = employee.FullName,
                employeeCode = employee.EmployeeCode,
                time = punchTime.ToString("yyyy-MM-dd HH:mm:ss"),
                totalHours = attendance.TotalHours,
                overtimeHours = attendance.OvertimeHours,
                message = $"Punch recorded for {employee.FullName} ({actionType}) at {punchTime:hh:mm tt}."
            });
        }

        // =========================================================================
        // 2. STANDARD eSSL / ZKTeco / ADMS PUSH PROTOCOL HANDSHAKE
        // Endpoints: GET /iclock/cdata & POST /iclock/cdata
        // =========================================================================
        [HttpGet("iclock/cdata")]
        public async Task<IActionResult> ZkHandshake([FromQuery] string? SN)
        {
            if (!string.IsNullOrWhiteSpace(SN))
            {
                var device = await _context.BiometricDevices.FirstOrDefaultAsync(d => d.DeviceIdentifier == SN);
                if (device != null)
                {
                    device.LastHeartbeat = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            // Standard ZK protocol acknowledgment
            return Content("OK", "text/plain");
        }

        [HttpPost("iclock/cdata")]
        public async Task<IActionResult> ZkReceiveLogs([FromQuery] string? SN, [FromQuery] string? table)
        {
            var device = !string.IsNullOrWhiteSpace(SN)
                ? await _context.BiometricDevices.FirstOrDefaultAsync(d => d.DeviceIdentifier == SN)
                : null;

            if (device != null)
            {
                device.LastHeartbeat = DateTime.Now;
            }

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var bodyText = await reader.ReadToEndAsync();

            int processedCount = 0;

            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                var lines = bodyText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    // Format: PIN\tTime\tStatus\tVerify\tWorkcode\tReserved
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                    {
                        var pin = parts[0].Trim();
                        if (DateTime.TryParse(parts[1].Trim(), out var pTime))
                        {
                            var dto = new BiometricPushDto
                            {
                                EmployeeCode = pin,
                                PunchTime = pTime,
                                DeviceIdentifier = SN,
                                VerificationType = "Face"
                            };
                            await UniversalPush(dto);
                            processedCount++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Content($"OK: {processedCount}", "text/plain");
        }

        [HttpGet("iclock/getrequest")]
        public IActionResult ZkGetRequest([FromQuery] string? SN)
        {
            // Empty command queue for device
            return Content("OK", "text/plain");
        }

        // =========================================================================
        // 3. HEALTH / PING ENDPOINT
        // =========================================================================
        [HttpGet("api/biometric/health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "online",
                system = "Aashana Fashion Biometric Gateway",
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }
}
