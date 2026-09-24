using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployeeController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Employee
        public async Task<IActionResult> Index(string? search, string? department, bool? activeOnly = true)
        {
            var query = _context.Employees.AsQueryable();

            if (activeOnly == true)
            {
                query = query.Where(e => e.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.FullName.Contains(search) || e.EmployeeCode.Contains(search) || e.Designation.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(e => e.Department == department);
            }

            var employees = await query.OrderBy(e => e.EmployeeCode).ToListAsync();

            ViewBag.Departments = await _context.Employees
                .Select(e => e.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Department = department;
            ViewBag.ActiveOnly = activeOnly;

            return View(employees);
        }

        // GET: /Employee/Create
        public IActionResult Create()
        {
            // Auto generate next code e.g. EMP-001
            var lastEmp = _context.Employees.OrderByDescending(e => e.Id).FirstOrDefault();
            var nextId = (lastEmp?.Id ?? 0) + 1;
            var defaultCode = $"EMP-{nextId:D3}";

            var model = new Employee
            {
                EmployeeCode = defaultCode,
                JoiningDate = DateTime.Today,
                StandardDailyHours = 8.0m,
                SalaryType = SalaryType.DailyWage,
                BaseRate = 600.0m,
                OvertimeHourlyRate = 100.0m
            };

            return View(model);
        }

        // POST: /Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode))
            {
                ModelState.AddModelError("EmployeeCode", "An employee with this Employee Code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            // Auto-calculate overtime rate if 0
            if (employee.OvertimeHourlyRate <= 0 && employee.BaseRate > 0)
            {
                if (employee.SalaryType == SalaryType.DailyWage)
                {
                    employee.OvertimeHourlyRate = Math.Round((employee.BaseRate / (employee.StandardDailyHours > 0 ? employee.StandardDailyHours : 8)) * 1.5m, 2);
                }
                else if (employee.SalaryType == SalaryType.MonthlySalary)
                {
                    employee.OvertimeHourlyRate = Math.Round((employee.BaseRate / 26m / 8m) * 1.5m, 2);
                }
                else
                {
                    employee.OvertimeHourlyRate = Math.Round(employee.BaseRate * 1.5m, 2);
                }
            }

            employee.CreatedAt = DateTime.Now;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Employee '{employee.FullName}' ({employee.EmployeeCode}) created successfully. You can now register their face.";
            return RedirectToAction(nameof(EnrollFace), new { id = employee.Id });
        }

        // GET: /Employee/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: /Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee model)
        {
            if (id != model.Id) return BadRequest();

            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode && e.Id != id))
            {
                ModelState.AddModelError("EmployeeCode", "Another employee already has this Employee Code.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.EmployeeCode = model.EmployeeCode;
            employee.FullName = model.FullName;
            employee.Designation = model.Designation;
            employee.Department = model.Department;
            employee.ContactNumber = model.ContactNumber;
            employee.Email = model.Email;
            employee.Address = model.Address;
            employee.JoiningDate = model.JoiningDate;
            employee.SalaryType = model.SalaryType;
            employee.BaseRate = model.BaseRate;
            employee.StandardDailyHours = model.StandardDailyHours;
            employee.OvertimeHourlyRate = model.OvertimeHourlyRate;
            employee.BankName = model.BankName;
            employee.BankAccountNumber = model.BankAccountNumber;
            employee.BankIFSC = model.BankIFSC;
            employee.UpiId = model.UpiId;
            employee.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Employee '{employee.FullName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Employee/EnrollFace/5
        public async Task<IActionResult> EnrollFace(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: /Employee/SaveFaceData
        [HttpPost]
        public async Task<IActionResult> SaveFaceData([FromBody] FaceEnrollmentDto dto)
        {
            if (dto == null || dto.EmployeeId <= 0 || string.IsNullOrWhiteSpace(dto.FaceDescriptor))
            {
                return Json(new { success = false, message = "Invalid face registration data." });
            }

            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            try
            {
                // Save photo snapshot if present
                if (!string.IsNullOrWhiteSpace(dto.FaceImageBase64))
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "faces");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = $"face_emp_{employee.Id}_{DateTime.Now.Ticks}.jpg";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    // strip data:image/jpeg;base64,
                    var base64Data = dto.FaceImageBase64;
                    var commaIndex = base64Data.IndexOf(',');
                    if (commaIndex >= 0)
                    {
                        base64Data = base64Data.Substring(commaIndex + 1);
                    }

                    var bytes = Convert.FromBase64String(base64Data);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                    employee.FacePhotoPath = $"/uploads/faces/{fileName}";
                }

                employee.FaceDescriptor = dto.FaceDescriptor;
                employee.IsFaceRegistered = true;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Face profile registered successfully for {employee.FullName}!",
                    photoPath = employee.FacePhotoPath
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving face data: " + ex.Message });
            }
        }

        // POST: /Employee/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = !employee.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Employee '{employee.FullName}' {(employee.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Employee/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AttendanceRecords)
                .Include(e => e.SalaryRecords)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee != null)
            {
                if (employee.AttendanceRecords.Any() || employee.SalaryRecords.Any())
                {
                    // Soft delete by deactivating
                    employee.IsActive = false;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Employee '{employee.FullName}' has attendance/salary records and was deactivated instead of deleted.";
                }
                else
                {
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Employee '{employee.FullName}' deleted.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
