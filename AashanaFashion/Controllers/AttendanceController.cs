using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AttendanceController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Attendance
        public async Task<IActionResult> Index(DateTime? date, string? department)
        {
            var targetDate = date?.Date ?? DateTime.Today;

            var empQuery = _context.Employees.Where(e => e.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(department))
            {
                empQuery = empQuery.Where(e => e.Department == department);
            }
            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

            var records = await _context.AttendanceRecords
                .Include(a => a.Employee)
                .Where(a => a.Date == targetDate)
                .ToListAsync();

            var recordDict = records.ToDictionary(r => r.EmployeeId, r => r);

            ViewBag.TargetDate = targetDate;
            ViewBag.Department = department;
            ViewBag.Departments = await _context.Employees.Select(e => e.Department).Distinct().OrderBy(d => d).ToListAsync();
            ViewBag.RecordDict = recordDict;

            // Metrics
            int totalActive = employees.Count;
            int presentCount = records.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
            int halfDayCount = records.Count(r => r.Status == AttendanceStatus.HalfDay);
            int lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
            int absentCount = totalActive - records.Count(r => r.Status != AttendanceStatus.Absent);

            ViewBag.TotalEmployees = totalActive;
            ViewBag.PresentCount = presentCount;
            ViewBag.HalfDayCount = halfDayCount;
            ViewBag.LateCount = lateCount;
            ViewBag.AbsentCount = Math.Max(0, absentCount);

            return View(employees);
        }

        // GET: /Attendance/Kiosk
        public IActionResult Kiosk()
        {
            return View();
        }

        // GET: /Attendance/GetRegisteredFaces (JSON endpoint for kiosk)
        [HttpGet]
        public async Task<IActionResult> GetRegisteredFaces()
        {
            var today = DateTime.Today;
            var employees = await _context.Employees
                .Where(e => e.IsActive && e.IsFaceRegistered && !string.IsNullOrEmpty(e.FaceDescriptor))
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeCode,
                    e.FullName,
                    e.Designation,
                    e.Department,
                    e.FacePhotoPath,
                    e.FaceDescriptor,
                    TodayRecord = e.AttendanceRecords.Where(a => a.Date == today).Select(a => new
                    {
                        a.Id,
                        a.CheckInTime,
                        a.CheckOutTime,
                        a.Status
                    }).FirstOrDefault()
                })
                .ToListAsync();

            var result = employees.Select(e => new EmployeeFaceProfileDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Designation = e.Designation,
                Department = e.Department,
                FacePhotoPath = e.FacePhotoPath,
                FaceDescriptor = e.FaceDescriptor,
                IsCheckedInToday = e.TodayRecord != null,
                TodayCheckInTime = e.TodayRecord?.CheckInTime.ToString("hh:mm tt"),
                TodayCheckOutTime = e.TodayRecord?.CheckOutTime?.ToString("hh:mm tt")
            }).ToList();

            return Json(result);
        }

        // POST: /Attendance/RecordPunch
        [HttpPost]
        public async Task<IActionResult> RecordPunch([FromBody] RecordPunchDto dto)
        {
            if (dto == null || dto.EmployeeId <= 0)
            {
                return Json(new { success = false, message = "Invalid punch request." });
            }

            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null || !employee.IsActive)
            {
                return Json(new { success = false, message = "Active employee not found." });
            }

            var today = DateTime.Today;
            var now = DateTime.Now;

            // Check if record exists for today
            var attendance = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.Date == today);

            string? snapshotPath = null;
            if (!string.IsNullOrWhiteSpace(dto.SnapshotBase64))
            {
                try
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "attendance");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"punch_{employee.Id}_{DateTime.Now.Ticks}.jpg";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    var base64Data = dto.SnapshotBase64;
                    var commaIndex = base64Data.IndexOf(',');
                    if (commaIndex >= 0) base64Data = base64Data.Substring(commaIndex + 1);

                    var bytes = Convert.FromBase64String(base64Data);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                    snapshotPath = $"/uploads/attendance/{fileName}";
                }
                catch
                {
                    // Non-fatal if photo snapshot write fails
                }
            }

            Enum.TryParse<VerificationMethod>(dto.VerificationMethod, true, out var method);

            if (attendance == null)
            {
                // PUNCH IN
                attendance = new AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    Date = today,
                    CheckInTime = now,
                    CheckOutTime = null,
                    VerificationMethod = method,
                    FaceConfidence = dto.FaceConfidence,
                    CheckInPhoto = snapshotPath,
                    TotalHours = 0,
                    OvertimeHours = 0
                };

                // Determine if late (e.g. after 09:30 AM)
                var standardStartTime = today.AddHours(9.5); // 9:30 AM
                if (now > standardStartTime)
                {
                    attendance.Status = AttendanceStatus.Late;
                    attendance.Notes = $"Late check-in at {now:hh:mm tt}";
                }
                else
                {
                    attendance.Status = AttendanceStatus.Present;
                }

                _context.AttendanceRecords.Add(attendance);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    punchType = "IN",
                    employeeName = employee.FullName,
                    employeeCode = employee.EmployeeCode,
                    time = now.ToString("hh:mm:ss tt"),
                    status = attendance.Status.ToString(),
                    message = $"Good day, {employee.FullName}! Check-In recorded at {now:hh:mm tt}."
                });
            }
            else if (attendance.CheckOutTime == null)
            {
                // PUNCH OUT
                attendance.CheckOutTime = now;
                attendance.CheckOutPhoto = snapshotPath;

                // Calculate working hours
                var duration = now - attendance.CheckInTime;
                var totalHours = Math.Round((decimal)duration.TotalHours, 2);
                attendance.TotalHours = Math.Max(0, totalHours);

                var stdHours = employee.StandardDailyHours > 0 ? employee.StandardDailyHours : 8.0m;
                if (attendance.TotalHours > stdHours)
                {
                    attendance.OvertimeHours = Math.Round(attendance.TotalHours - stdHours, 2);
                }
                else
                {
                    attendance.OvertimeHours = 0;
                }

                // If worked less than 4 hours, mark as HalfDay unless already Late
                if (attendance.TotalHours < 4.0m)
                {
                    attendance.Status = AttendanceStatus.HalfDay;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    punchType = "OUT",
                    employeeName = employee.FullName,
                    employeeCode = employee.EmployeeCode,
                    time = now.ToString("hh:mm:ss tt"),
                    totalHours = attendance.TotalHours,
                    overtimeHours = attendance.OvertimeHours,
                    status = attendance.Status.ToString(),
                    message = $"Goodbye, {employee.FullName}! Check-Out recorded at {now:hh:mm tt}. Total: {attendance.TotalHours} hrs."
                });
            }
            else
            {
                // Already punched in and out today
                return Json(new
                {
                    success = false,
                    isAlreadyDone = true,
                    employeeName = employee.FullName,
                    message = $"{employee.FullName} has already completed attendance today (In: {attendance.CheckInTime:hh:mm tt}, Out: {attendance.CheckOutTime.Value:hh:mm tt}, Total: {attendance.TotalHours} hrs)."
                });
            }
        }

        // POST: /Attendance/ManualPunch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualPunch(ManualPunchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid attendance entry.";
                return RedirectToAction(nameof(Index), new { date = model.Date.ToString("yyyy-MM-dd") });
            }

            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null) return NotFound();

            var checkIn = model.Date.Date + model.CheckInTime;
            DateTime? checkOut = model.CheckOutTime.HasValue ? model.Date.Date + model.CheckOutTime.Value : null;

            decimal totalHours = 0m;
            decimal overtimeHours = 0m;

            if (checkOut.HasValue && checkOut > checkIn)
            {
                totalHours = Math.Round((decimal)(checkOut.Value - checkIn).TotalHours, 2);
                var stdHours = employee.StandardDailyHours > 0 ? employee.StandardDailyHours : 8.0m;
                if (totalHours > stdHours)
                {
                    overtimeHours = Math.Round(totalHours - stdHours, 2);
                }
            }

            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.EmployeeId == model.EmployeeId && a.Date == model.Date.Date);

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = model.EmployeeId,
                    Date = model.Date.Date,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    TotalHours = totalHours,
                    OvertimeHours = overtimeHours,
                    Status = model.Status,
                    VerificationMethod = VerificationMethod.ManualPunch,
                    Notes = model.Notes
                };
                _context.AttendanceRecords.Add(record);
            }
            else
            {
                record.CheckInTime = checkIn;
                record.CheckOutTime = checkOut;
                record.TotalHours = totalHours;
                record.OvertimeHours = overtimeHours;
                record.Status = model.Status;
                record.VerificationMethod = VerificationMethod.ManualPunch;
                if (!string.IsNullOrWhiteSpace(model.Notes))
                {
                    record.Notes = model.Notes;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Attendance updated manually for {employee.FullName}.";
            return RedirectToAction(nameof(Index), new { date = model.Date.ToString("yyyy-MM-dd") });
        }

        // GET: /Attendance/MonthlyReport
        public async Task<IActionResult> MonthlyReport(int? year, int? month, string? department)
        {
            var curYear = year ?? DateTime.Today.Year;
            var curMonth = month ?? DateTime.Today.Month;

            var startDate = new DateTime(curYear, curMonth, 1);
            var daysInMonth = DateTime.DaysInMonth(curYear, curMonth);
            var endDate = new DateTime(curYear, curMonth, daysInMonth);

            var empQuery = _context.Employees.Where(e => e.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(department))
            {
                empQuery = empQuery.Where(e => e.Department == department);
            }
            var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync();

            var records = await _context.AttendanceRecords
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            ViewBag.Year = curYear;
            ViewBag.Month = curMonth;
            ViewBag.DaysInMonth = daysInMonth;
            ViewBag.Department = department;
            ViewBag.Departments = await _context.Employees.Select(e => e.Department).Distinct().OrderBy(d => d).ToListAsync();
            ViewBag.Records = records;

            return View(employees);
        }
    }
}
