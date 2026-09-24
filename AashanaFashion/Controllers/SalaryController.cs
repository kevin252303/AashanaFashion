using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly AppDbContext _context;

        public SalaryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Salary
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var curYear = year ?? DateTime.Today.Year;
            var curMonth = month ?? DateTime.Today.Month;

            var records = await _context.SalaryRecords
                .Include(s => s.Employee)
                .Where(s => s.Year == curYear && s.Month == curMonth)
                .OrderBy(s => s.Employee!.EmployeeCode)
                .ToListAsync();

            var viewModel = new PayrollSummaryViewModel
            {
                Year = curYear,
                Month = curMonth,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(curMonth),
                TotalEmployees = records.Count,
                TotalGrossSalary = records.Sum(r => r.BaseSalaryEarned),
                TotalOvertimePay = records.Sum(r => r.OvertimePay),
                TotalDeductions = records.Sum(r => r.Deductions),
                TotalNetPayable = records.Sum(r => r.NetSalary),
                PaidCount = records.Count(r => r.PaymentStatus == PayrollStatus.Paid),
                PendingCount = records.Count(r => r.PaymentStatus != PayrollStatus.Paid),
                Records = records
            };

            return View(viewModel);
        }

        // POST: /Salary/GeneratePayroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePayroll(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var endDate = new DateTime(year, month, daysInMonth);

            // Total working days in month (excluding Sundays)
            int totalWorkingDays = 0;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek != DayOfWeek.Sunday) totalWorkingDays++;
            }
            if (totalWorkingDays == 0) totalWorkingDays = 26;

            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .Include(e => e.AttendanceRecords.Where(a => a.Date >= startDate && a.Date <= endDate))
                .ToListAsync();

            int generatedCount = 0;

            foreach (var emp in employees)
            {
                var attList = emp.AttendanceRecords.ToList();

                var fullDays = attList.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
                var halfDays = attList.Count(a => a.Status == AttendanceStatus.HalfDay);
                decimal daysPresent = fullDays + (halfDays * 0.5m);
                decimal daysAbsent = Math.Max(0, totalWorkingDays - daysPresent);

                decimal totalHoursWorked = attList.Sum(a => a.TotalHours);
                decimal totalOvertimeHours = attList.Sum(a => a.OvertimeHours);

                decimal baseSalaryEarned = 0m;

                switch (emp.SalaryType)
                {
                    case SalaryType.MonthlySalary:
                        // If attended all working days or more, full salary; else prorated
                        if (daysPresent >= totalWorkingDays)
                        {
                            baseSalaryEarned = emp.BaseRate;
                        }
                        else
                        {
                            baseSalaryEarned = Math.Round((emp.BaseRate / (decimal)totalWorkingDays) * daysPresent, 2);
                        }
                        break;

                    case SalaryType.DailyWage:
                        baseSalaryEarned = Math.Round(emp.BaseRate * daysPresent, 2);
                        break;

                    case SalaryType.HourlyRate:
                        baseSalaryEarned = Math.Round(emp.BaseRate * totalHoursWorked, 2);
                        break;
                }

                // Overtime pay
                decimal otRate = emp.OvertimeHourlyRate > 0
                    ? emp.OvertimeHourlyRate
                    : (emp.SalaryType == SalaryType.DailyWage ? (emp.BaseRate / 8m) * 1.5m : 100m);

                decimal overtimePay = Math.Round(otRate * totalOvertimeHours, 2);

                // Find existing record or create new
                var record = await _context.SalaryRecords
                    .FirstOrDefaultAsync(s => s.EmployeeId == emp.Id && s.Year == year && s.Month == month);

                if (record == null)
                {
                    record = new SalaryRecord
                    {
                        EmployeeId = emp.Id,
                        Year = year,
                        Month = month,
                        GeneratedDate = DateTime.Now,
                        TotalWorkingDays = totalWorkingDays,
                        DaysPresent = daysPresent,
                        HalfDays = halfDays,
                        DaysAbsent = daysAbsent,
                        TotalHoursWorked = totalHoursWorked,
                        TotalOvertimeHours = totalOvertimeHours,
                        BaseSalaryEarned = baseSalaryEarned,
                        OvertimePay = overtimePay,
                        BonusAllowance = 0m,
                        Deductions = 0m,
                        NetSalary = baseSalaryEarned + overtimePay,
                        PaymentStatus = PayrollStatus.Pending
                    };
                    _context.SalaryRecords.Add(record);
                }
                else
                {
                    // Update attendance numbers and base earned, retain previous bonus/deductions
                    record.TotalWorkingDays = totalWorkingDays;
                    record.DaysPresent = daysPresent;
                    record.HalfDays = halfDays;
                    record.DaysAbsent = daysAbsent;
                    record.TotalHoursWorked = totalHoursWorked;
                    record.TotalOvertimeHours = totalOvertimeHours;
                    record.BaseSalaryEarned = baseSalaryEarned;
                    record.OvertimePay = overtimePay;
                    record.NetSalary = baseSalaryEarned + overtimePay + record.BonusAllowance - record.Deductions;
                    record.GeneratedDate = DateTime.Now;
                }

                generatedCount++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Payroll for {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} {year} calculated successfully for {generatedCount} employees.";
            return RedirectToAction(nameof(Index), new { year, month });
        }

        // GET: /Salary/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.SalaryRecords
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // POST: /Salary/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SalaryRecord model)
        {
            if (id != model.Id) return BadRequest();

            var record = await _context.SalaryRecords
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (record == null) return NotFound();

            record.BaseSalaryEarned = model.BaseSalaryEarned;
            record.OvertimePay = model.OvertimePay;
            record.BonusAllowance = model.BonusAllowance;
            record.Deductions = model.Deductions;
            record.NetSalary = record.BaseSalaryEarned + record.OvertimePay + record.BonusAllowance - record.Deductions;
            record.Remarks = model.Remarks;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Salary record for {record.Employee?.FullName} updated.";
            return RedirectToAction(nameof(Index), new { year = record.Year, month = record.Month });
        }

        // POST: /Salary/MarkPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id, string paymentMethod, string? paymentRef)
        {
            var record = await _context.SalaryRecords
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (record == null) return NotFound();

            record.PaymentStatus = PayrollStatus.Paid;
            record.PaidDate = DateTime.Now;
            record.PaymentMethod = paymentMethod;
            record.PaymentReference = paymentRef;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Salary of ₹{record.NetSalary:N2} marked as PAID for {record.Employee?.FullName}.";
            return RedirectToAction(nameof(Index), new { year = record.Year, month = record.Month });
        }

        // GET: /Salary/Payslip/5
        public async Task<IActionResult> Payslip(int id)
        {
            var record = await _context.SalaryRecords
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (record == null) return NotFound();

            ViewBag.MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Month);
            return View(record);
        }
    }
}
