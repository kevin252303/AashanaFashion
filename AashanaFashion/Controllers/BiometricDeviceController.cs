using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class BiometricDeviceController : Controller
    {
        private readonly AppDbContext _context;

        public BiometricDeviceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /BiometricDevice
        public async Task<IActionResult> Index(string? search, bool? activeOnly)
        {
            var query = _context.BiometricDevices
                .Include(d => d.AttendanceRecords)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d =>
                    d.DeviceName.ToLower().Contains(s) ||
                    d.DeviceIdentifier.ToLower().Contains(s) ||
                    (d.Location != null && d.Location.ToLower().Contains(s)) ||
                    (d.IpAddress != null && d.IpAddress.ToLower().Contains(s)) ||
                    (d.DeviceModel != null && d.DeviceModel.ToLower().Contains(s)));
            }

            if (activeOnly == true)
            {
                query = query.Where(d => d.IsActive);
            }

            var devices = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            ViewBag.BaseUrl = baseUrl;
            ViewBag.Search = search;
            ViewBag.ActiveOnly = activeOnly ?? false;

            return View(devices);
        }

        // GET: /BiometricDevice/Create
        public IActionResult Create()
        {
            var model = new BiometricDevice
            {
                DeviceName = "Main Floor Face Terminal",
                DeviceIdentifier = $"DEV-{DateTime.Now:yyyyMMddHHmm}",
                ApiKey = Guid.NewGuid().ToString("N"),
                DeviceModel = "Universal Face Recognition Terminal",
                Location = "Factory Floor - Main Gate"
            };
            return View(model);
        }

        // POST: /BiometricDevice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BiometricDevice device)
        {
            if (await _context.BiometricDevices.AnyAsync(d => d.DeviceIdentifier == device.DeviceIdentifier))
            {
                ModelState.AddModelError("DeviceIdentifier", "A device with this Serial Number / Identifier is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return View(device);
            }

            if (string.IsNullOrWhiteSpace(device.ApiKey))
            {
                device.ApiKey = Guid.NewGuid().ToString("N");
            }

            device.CreatedAt = DateTime.Now;
            _context.BiometricDevices.Add(device);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Device '{device.DeviceName}' registered successfully. Configure its Webhook URL in device settings.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /BiometricDevice/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var device = await _context.BiometricDevices.FindAsync(id);
            if (device == null) return NotFound();

            var request = HttpContext.Request;
            ViewBag.BaseUrl = $"{request.Scheme}://{request.Host}";
            return View(device);
        }

        // POST: /BiometricDevice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BiometricDevice model)
        {
            if (id != model.Id) return BadRequest();

            if (await _context.BiometricDevices.AnyAsync(d => d.DeviceIdentifier == model.DeviceIdentifier && d.Id != id))
            {
                ModelState.AddModelError("DeviceIdentifier", "Another device already has this Identifier.");
            }

            if (!ModelState.IsValid)
            {
                var request = HttpContext.Request;
                ViewBag.BaseUrl = $"{request.Scheme}://{request.Host}";
                return View(model);
            }

            var device = await _context.BiometricDevices.FindAsync(id);
            if (device == null) return NotFound();

            device.DeviceName = model.DeviceName;
            device.DeviceIdentifier = model.DeviceIdentifier;
            device.Location = model.Location;
            device.IpAddress = model.IpAddress;
            device.DeviceModel = model.DeviceModel;
            device.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Device '{device.DeviceName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /BiometricDevice/RegenerateApiKey/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateApiKey(int id)
        {
            var device = await _context.BiometricDevices.FindAsync(id);
            if (device != null)
            {
                device.ApiKey = Guid.NewGuid().ToString("N");
                await _context.SaveChangesAsync();
                TempData["Success"] = $"New API Key generated for '{device.DeviceName}'.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /BiometricDevice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var device = await _context.BiometricDevices.FindAsync(id);
            if (device != null)
            {
                _context.BiometricDevices.Remove(device);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Device '{device.DeviceName}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /BiometricDevice/ImportLogs
        public IActionResult ImportLogs()
        {
            return View();
        }

        // POST: /BiometricDevice/ImportLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportLogs(IFormFile logFile, int? deviceId)
        {
            if (logFile == null || logFile.Length == 0)
            {
                TempData["Error"] = "Please select a valid log file (.csv, .txt, .dat).";
                return View();
            }

            int importedCount = 0;
            int skippedCount = 0;
            var errorList = new List<string>();

            using (var reader = new StreamReader(logFile.OpenReadStream(), Encoding.UTF8))
            {
                string? line;
                int lineNumber = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Skip header line if starts with "Emp" or "Pin" or "User"
                    if (lineNumber == 1 && (line.StartsWith("Emp", StringComparison.OrdinalIgnoreCase) || 
                                            line.StartsWith("Pin", StringComparison.OrdinalIgnoreCase) || 
                                            line.StartsWith("User", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    // Delimiter can be comma, tab, or semicolon
                    char delimiter = line.Contains('\t') ? '\t' : (line.Contains(',') ? ',' : ';');
                    var parts = line.Split(delimiter);

                    if (parts.Length < 2)
                    {
                        skippedCount++;
                        continue;
                    }

                    var rawEmpCode = parts[0].Trim().Trim('"', '\'');
                    var rawTime = parts[1].Trim().Trim('"', '\'');

                    if (!DateTime.TryParse(rawTime, out var punchTime))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Resolve employee
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeCode == rawEmpCode || 
                                                  e.EmployeeCode == $"EMP-{rawEmpCode}" ||
                                                  e.EmployeeCode.EndsWith($"-{rawEmpCode.PadLeft(3, '0')}"));

                    if (employee == null)
                    {
                        if (errorList.Count < 5) errorList.Add($"Line {lineNumber}: Employee '{rawEmpCode}' not found.");
                        skippedCount++;
                        continue;
                    }

                    // Check duplicate punch
                    var punchDate = punchTime.Date;
                    var att = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == punchDate);

                    if (att == null)
                    {
                        // Check In
                        att = new AttendanceRecord
                        {
                            EmployeeId = employee.Id,
                            Date = punchDate,
                            CheckInTime = punchTime,
                            CheckOutTime = null,
                            VerificationMethod = VerificationMethod.BiometricDevice,
                            DeviceId = deviceId,
                            Status = punchTime.TimeOfDay > new TimeSpan(9, 30, 0) ? AttendanceStatus.Late : AttendanceStatus.Present,
                            Notes = "Imported from device log file"
                        };
                        _context.AttendanceRecords.Add(att);
                        importedCount++;
                    }
                    else if (att.CheckOutTime == null && punchTime > att.CheckInTime)
                    {
                        // Check Out
                        att.CheckOutTime = punchTime;
                        var duration = punchTime - att.CheckInTime;
                        att.TotalHours = Math.Round((decimal)duration.TotalHours, 2);
                        var stdHours = employee.StandardDailyHours > 0 ? employee.StandardDailyHours : 8.0m;
                        att.OvertimeHours = att.TotalHours > stdHours ? Math.Round(att.TotalHours - stdHours, 2) : 0;
                        if (att.TotalHours < 4.0m && att.Status != AttendanceStatus.Late) att.Status = AttendanceStatus.HalfDay;
                        importedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                await _context.SaveChangesAsync();
            }

            ViewBag.ImportedCount = importedCount;
            ViewBag.SkippedCount = skippedCount;
            ViewBag.Errors = errorList;
            ViewBag.Success = true;

            TempData["Success"] = $"Log import completed: {importedCount} punches recorded, {skippedCount} rows skipped.";
            return View();
        }

        // POST: /BiometricDevice/TestPunch
        [HttpPost]
        public async Task<IActionResult> TestPunch(string employeeCode, string? punchType)
        {
            var apiController = new BiometricApiController(_context, null!);
            var dto = new BiometricPushDto
            {
                EmployeeCode = employeeCode,
                PunchTime = DateTime.Now,
                PunchType = punchType ?? "Auto",
                VerificationType = "Face (Simulator)",
                ConfidenceScore = 98.5
            };

            var result = await apiController.UniversalPush(dto);
            return result;
        }
    }
}
