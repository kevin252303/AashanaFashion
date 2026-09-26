using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;

namespace AashanaFashion.Controllers;

[Authorize]
public class CategoryController : Controller
{
    private readonly AppDbContext _context;

    public CategoryController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        var query = _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.CategoryName.ToLower().Contains(term) ||
                                     (c.CategoryCode != null && c.CategoryCode.ToLower().Contains(term)) ||
                                     (c.DefaultHsnCode != null && c.DefaultHsnCode.ToLower().Contains(term)) ||
                                     (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var list = await query.OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Search = search;
        ViewBag.ActiveOnly = activeOnly;

        return View(list);
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadParentCategoriesAsync();
        return View(new ProductCategory { IsActive = true, DefaultGstRate = 5.0m });
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.CategoryName))
        {
            ModelState.AddModelError("CategoryName", "Category Name is required.");
        }
        else
        {
            var exists = await _context.ProductCategories.AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("CategoryName", "A category with this name already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync();
            return View(category);
        }

        category.CategoryName = category.CategoryName.Trim();
        category.CategoryCode = category.CategoryCode?.Trim().ToUpper();
        category.DefaultHsnCode = category.DefaultHsnCode?.Trim();
        category.Description = category.Description?.Trim();
        category.CreatedDate = DateTime.Now;

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.CategoryName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        await LoadParentCategoriesAsync(excludeId: id);
        return View(category);
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.CategoryName))
        {
            ModelState.AddModelError("CategoryName", "Category Name is required.");
        }
        else
        {
            var exists = await _context.ProductCategories
                .AnyAsync(c => c.Id != category.Id && c.CategoryName.ToLower() == category.CategoryName.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("CategoryName", "Another category with this name already exists.");
            }
        }

        if (category.ParentCategoryId == category.Id)
        {
            ModelState.AddModelError("ParentCategoryId", "A category cannot be its own parent.");
        }

        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync(excludeId: category.Id);
            return View(category);
        }

        var dbCategory = await _context.ProductCategories.FindAsync(category.Id);
        if (dbCategory == null) return NotFound();

        var oldName = dbCategory.CategoryName;
        dbCategory.CategoryName = category.CategoryName.Trim();
        dbCategory.CategoryCode = category.CategoryCode?.Trim().ToUpper();
        dbCategory.ParentCategoryId = category.ParentCategoryId;
        dbCategory.DefaultHsnCode = category.DefaultHsnCode?.Trim();
        dbCategory.DefaultGstRate = category.DefaultGstRate;
        dbCategory.Description = category.Description?.Trim();
        dbCategory.IsActive = category.IsActive;

        // If category name changed, update existing Design.Category string for synchronization
        if (!string.Equals(oldName, dbCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
        {
            var linkedDesigns = await _context.Designs.Where(d => d.CategoryId == dbCategory.Id).ToListAsync();
            foreach (var d in linkedDesigns)
            {
                d.Category = dbCategory.CategoryName;
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{dbCategory.CategoryName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null) return NotFound();

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.CategoryName}' is now {(category.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("DesignMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.ProductCategories
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        if (category.Products.Any())
        {
            TempData["Error"] = $"Cannot delete category '{category.CategoryName}' because {category.Products.Count} product(s) are assigned to it. Please reassign the products first or mark the category inactive.";
            return RedirectToAction(nameof(Index));
        }

        if (category.SubCategories.Any())
        {
            TempData["Error"] = $"Cannot delete category '{category.CategoryName}' because it has {category.SubCategories.Count} subcategory(s). Please reassign or delete subcategories first.";
            return RedirectToAction(nameof(Index));
        }

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Category '{category.CategoryName}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreateCategoryModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CategoryName))
        {
            return BadRequest(new { success = false, message = "Category Name is required." });
        }

        var trimmedName = model.CategoryName.Trim();
        var existing = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == trimmedName.ToLower());

        if (existing != null)
        {
            return Ok(new
            {
                success = true,
                id = existing.Id,
                name = existing.CategoryName,
                code = existing.CategoryCode,
                hsn = existing.DefaultHsnCode,
                gst = existing.DefaultGstRate,
                isExisting = true
            });
        }

        var category = new ProductCategory
        {
            CategoryName = trimmedName,
            CategoryCode = model.CategoryCode?.Trim().ToUpper(),
            ParentCategoryId = model.ParentCategoryId > 0 ? model.ParentCategoryId : null,
            DefaultHsnCode = model.DefaultHsnCode?.Trim(),
            DefaultGstRate = model.DefaultGstRate ?? 5.0m,
            Description = model.Description?.Trim(),
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            id = category.Id,
            name = category.CategoryName,
            code = category.CategoryCode,
            hsn = category.DefaultHsnCode,
            gst = category.DefaultGstRate,
            isExisting = false
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null) return NotFound();

        return Json(new
        {
            id = category.Id,
            name = category.CategoryName,
            code = category.CategoryCode,
            hsn = category.DefaultHsnCode,
            gst = category.DefaultGstRate
        });
    }

    private async Task LoadParentCategoriesAsync(int? excludeId = null)
    {
        var query = _context.ProductCategories.Where(c => c.IsActive);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        ViewBag.ParentCategories = await query.OrderBy(c => c.CategoryName).ToListAsync();
    }
}
