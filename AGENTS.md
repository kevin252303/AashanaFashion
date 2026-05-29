# AGENTS.md — Aashana Fashion Development Guide

This document provides guidance for AI agents working on the Aashana Fashion codebase.

## Project Overview

- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: Cookie-based with role authorization (Admin, Manager, Viewer)
- **Frontend**: Razor Views with Bootstrap 5

## Build & Development Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Run the application (https://localhost:5001, http://localhost:5000)
dotnet run

# Run with specific URL
dotnet run --urls "http://localhost:3000"

# Watch mode (auto-rebuild on changes)
dotnet watch run

# Clean build artifacts
dotnet clean
```

## Testing Commands

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run tests in Release configuration
dotnet test --configuration Release

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Database Commands

```bash
# Apply migrations
dotnet ef database update

# Create a new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove

# Scaffold DbContext from existing database
dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Data

# Drop database and recreate
dotnet ef database drop --force && dotnet ef database update
```

## Linting & Code Analysis

```bash
# Run Roslyn analyzers (built-in with SDK)
dotnet build

# Format code (requires .NET SDK)
dotnet format

# Check formatting without applying
dotnet format --verify-no-changes
```

## Code Style Guidelines

### General Conventions

1. **Enable nullable reference types** (`<Nullable>enable</Nullable>` in csproj)
   - Always initialize string properties: `public string Name { get; set; } = string.Empty;`
   - Use `string?` for nullable strings
   - Use `?` for nullable value types where applicable

2. **Use file-scoped namespaces**
   ```csharp
   namespace AashanaFashion.Controllers;  // File-scoped (preferred)
   ```

3. **Use target-typed new expressions**
   ```csharp
   var user = new AppUser();           // Preferred
   var user = new AppUser { ... };     // With initialization
   ```

4. **Primary constructors** (C# 12+) for simple DI
   ```csharp
   public AccountController(AppDbContext context) => _context = context;
   ```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `ProductionOrder`, `AppDbContext` |
| Methods | PascalCase | `CalculateProgress`, `ToggleActive` |
| Properties | PascalCase | `DesignNumber`, `IsActive` |
| Fields | _camelCase | `_context`, `_logger` |
| Parameters | camelCase | `model`, `returnUrl` |
| Local variables | camelCase | `orders`, `user` |
| Enums | PascalCase | `OrderStatus.ReadyToDispatch` |
| Enum values | PascalCase | `AtStitching`, `AtDying` |
| Controllers | PascalCase + "Controller" suffix | `ProductionController` |
| ViewModels | PascalCase + "ViewModel" suffix | `LoginViewModel`, `BatchDashboardViewModel` |

### Import Organization

Order imports as follows:
1. System namespaces (`using System...`)
2. Third-party namespaces (`using Microsoft...`, NuGet packages)
3. Project namespaces (`using AashanaFashion...`)

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
```

### Controller Guidelines

1. **Use attribute routing with authorization**
   ```csharp
   [Authorize]                        // All authenticated users
   [Authorize(Roles = "Admin")]      // Admin only
   [Authorize(Roles = "Admin,Manager")]  // Multiple roles
   ```

2. **Always add anti-forgery tokens to POST actions**
   ```csharp
   [HttpPost]
   [ValidateAntiForgeryToken]
   public async Task<IActionResult> ActionName(...) { }
   ```

3. **Use async/await for database operations**
   ```csharp
   public async Task<IActionResult> Index()
   {
       var orders = await _context.ProductionOrders.ToListAsync();
       return View(orders);
   }
   ```

4. **Return appropriate HTTP status codes**
   - `return View(model)` — 200 OK
   - `return NotFound()` — 404 Not Found
   - `return RedirectToAction(...)` — 302 Redirect

5. **Constructor injection for dependencies**
   ```csharp
   private readonly AppDbContext _context;
   public ProductionController(AppDbContext context) => _context = context;
   ```

### Model Guidelines

1. **Use enums for status/states**
   ```csharp
   public enum OrderStatus
   {
       RawMaterialArrived,
       AtDying,
       AtHandwork,
       AtStitching,
       ReadyToDispatch,
       Dispatched
   }
   ```

2. **Use data annotations for validation**
   ```csharp
   [Required]
   [DataType(DataType.Password)]
   [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
   public string? Password { get; set; }
   ```

3. **Initialize collections and strings with defaults**
   ```csharp
   public string DesignNumber { get; set; } = string.Empty;
   public int TotalQuantity { get; set; }
   ```

4. **Use computed properties for convenience**
   ```csharp
   public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
   ```

### Entity Framework Guidelines

1. **Configure entity relationships in `OnModelCreating`**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<AppUser>()
           .HasIndex(u => u.Username)
           .IsUnique();
   }
   ```

2. **Use async methods for queries**
   ```csharp
   var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
   ```

3. **Check null before operations**
   ```csharp
   var order = await _context.ProductionOrders.FindAsync(id);
   if (order == null) return NotFound();
   ```

### View Guidelines

1. **Use Tag Helpers for links and forms**
   ```html
   <a asp-controller="Production" asp-action="Index">Production</a>
   <form asp-controller="Account" asp-action="Logout" method="post">
   ```

2. **Always include anti-forgery token in forms**
   ```html
   @Html.AntiForgeryToken()
   ```

3. **Use ViewData for passing data to layout**
   ```csharp
   ViewData["Title"] = "Production Orders";
   ```

### Error Handling

1. **Use ModelState for form validation errors**
   ```csharp
   ModelState.AddModelError("Username", "Username already exists.");
   return View(model);
   ```

2. **Use TempData for post-action messages**
   ```csharp
   TempData["Success"] = "User created successfully.";
   ```

3. **Handle null navigation safely**
   ```csharp
   if (User.Identity?.IsAuthenticated == true) { }
   var fullName = User.FindFirst("FullName")?.Value ?? "";
   ```

### Security Guidelines

1. **Never store plain passwords** — use BCrypt
   ```csharp
   PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
   BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
   ```

2. **Validate return URLs for redirects**
   ```csharp
   if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
       return Redirect(returnUrl);
   ```

3. **Use `[Authorize]` on all controllers** unless explicitly allowing anonymous

## Project Structure

```
AashanaFashion/
├── Controllers/           # MVC Controllers
├── Models/               # Domain models and ViewModels
├── Data/                 # DbContext and data access
├── Views/                # Razor views
│   ├── Shared/           # Layouts and partial views
│   └── [Controller]/     # Controller-specific views
├── Migrations/           # EF Core migrations
└── wwwroot/              # Static assets
```

## Common Patterns

### Role-Based Access Control
- **Admin**: Full access (CRUD all resources)
- **Manager**: Read all, Update status
- **Viewer**: Read-only access

### Production Order Flow
```
RawMaterialArrived → AtDying → AtHandwork → AtStitching → ReadyToDispatch → Dispatched
```

### Default Seeded Users
| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | Admin |
| manager | manager123 | Manager |
| viewer | viewer123 | Viewer |
