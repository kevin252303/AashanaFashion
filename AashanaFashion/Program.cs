using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("AashanaFashion");

var activeConnection = builder.Configuration.GetValue<string>("ActiveConnection") ?? "DefaultConnection";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(activeConnection)));

builder.Services.AddHttpClient<IGstVerificationService, GstVerificationService>();
builder.Services.AddScoped<IEwayBillService, EwayBillService>();
builder.Services.AddScoped<IPricelistService, PricelistService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdClaim = context.Principal?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.IsActive)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed standard roles and module permissions in UserRoleList
    var allModules = new[]
    {
        "ProductionOrder", "DesignMaster", "VendorMaster", "CustomerMaster",
        "Purchase", "Dying", "RollPress", "PMS", "RawMaterial",
        "Inventory", "SalesOrder", "Invoice", "QualityControl", "Barcode",
        "Employee", "Attendance", "Salary", "BiometricDevice", "Accounting",
        "UserManagement", "Role"
    };

    var standardRoles = new[]
    {
        new { Name = "SuperAdmin",   Desc = "Super Administrator with full unrestricted access across all systems" },
        new { Name = "Admin",        Desc = "Company Administrator with full operational & management access" },
        new { Name = "System Admin", Desc = "System Administrator with specialized financial & accounting access" },
        new { Name = "Manager",      Desc = "Operations Manager for production, inventory, sales, QC, & HR" },
        new { Name = "Viewer",       Desc = "Read-only access for floor tracking, status views, and reports" },
    };

    foreach (var r in standardRoles)
    {
        var roleRec = db.UserRoles.FirstOrDefault(x => x.RoleName == r.Name);
        if (roleRec == null)
        {
            roleRec = new UserRole
            {
                RoleName = r.Name,
                Description = r.Desc,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            db.UserRoles.Add(roleRec);
            db.SaveChanges();
        }

        // Seed or update permissions for all 21 modules
        foreach (var mod in allModules)
        {
            var perm = db.RolePermissions.FirstOrDefault(p => p.UserRoleId == roleRec.Id && p.Module == mod);
            bool canView = true;
            bool canCreate = false;
            bool canEdit = false;
            bool canDelete = false;

            if (r.Name == "SuperAdmin" || r.Name == "Admin")
            {
                canCreate = true;
                canEdit = true;
                canDelete = true;
            }
            else if (r.Name == "System Admin")
            {
                canCreate = true;
                canEdit = true;
                canDelete = (mod == "Accounting" || mod == "Invoice");
            }
            else if (r.Name == "Manager")
            {
                bool isAdminOnlyMod = (mod == "UserManagement" || mod == "Role" || mod == "Accounting");
                if (!isAdminOnlyMod)
                {
                    canCreate = true;
                    canEdit = true;
                    canDelete = false;
                }
                else
                {
                    canView = false;
                }
            }
            else if (r.Name == "Viewer")
            {
                bool isAdminOnlyMod = (mod == "UserManagement" || mod == "Role" || mod == "Accounting" || mod == "Salary");
                canView = !isAdminOnlyMod;
            }

            if (perm == null)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    UserRoleId = roleRec.Id,
                    Module = mod,
                    CanView = canView,
                    CanCreate = canCreate,
                    CanEdit = canEdit,
                    CanDelete = canDelete
                });
            }
            else if (r.Name == "Admin" || r.Name == "SuperAdmin")
            {
                perm.CanView = true;
                perm.CanCreate = true;
                perm.CanEdit = true;
                perm.CanDelete = true;
            }
        }
        db.SaveChanges();
    }

    // Seed users
    var usersToSeed = new[]
    {
        new { Username = "superadmin", Password = "superadmin123", Role = "SuperAdmin", First = "Super",      Last = "Admin"   },
        new { Username = "sysadmin",    Password = "sysadmin123",    Role = "System Admin", First = "System",     Last = "Admin"   },
        new { Username = "admin",       Password = "admin123",       Role = "Admin",      First = "Admin",      Last = "User"    },
        new { Username = "manager",     Password = "manager123",     Role = "Manager",    First = "Production", Last = "Manager" },
        new { Username = "viewer",      Password = "viewer123",      Role = "Viewer",     First = "Floor",      Last = "Viewer"  },
    };
    foreach (var u in usersToSeed)
    {
        var existing = db.Users.FirstOrDefault(x => x.Username == u.Username);
        if (existing == null)
        {
            db.Users.Add(new AppUser
            {
                Username     = u.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password),
                Role         = u.Role,
                FirstName    = u.First,
                LastName     = u.Last,
                IsActive     = true
            });
        }
        else if (existing.Role != u.Role)
        {
            existing.Role = u.Role;
        }
    }
    db.SaveChanges();

    // Seed colours
    if (!db.Colours.Any())
    {
        db.Colours.AddRange(
            new Colour { ColourName = "Red", ColourCode = "#FF0000" },
            new Colour { ColourName = "Blue", ColourCode = "#0000FF" },
            new Colour { ColourName = "Green", ColourCode = "#008000" },
            new Colour { ColourName = "Yellow", ColourCode = "#FFFF00" },
            new Colour { ColourName = "Pink", ColourCode = "#FFC0CB" },
            new Colour { ColourName = "Black", ColourCode = "#000000" },
            new Colour { ColourName = "White", ColourCode = "#FFFFFF" },
            new Colour { ColourName = "Orange", ColourCode = "#FFA500" }
        );
        db.SaveChanges();
    }

    // Seed sizes
    if (!db.Sizes.Any())
    {
        db.Sizes.AddRange(
            new Size { SizeName = "XS", DisplayOrder = 1 },
            new Size { SizeName = "S", DisplayOrder = 2 },
            new Size { SizeName = "M", DisplayOrder = 3 },
            new Size { SizeName = "L", DisplayOrder = 4 },
            new Size { SizeName = "XL", DisplayOrder = 5 },
            new Size { SizeName = "XXL", DisplayOrder = 6 }
        );
        db.SaveChanges();
    }

    // Seed designs
    if (!db.Designs.Any())
    {
        db.Designs.AddRange(
            new Design { DesignNumber = "AF-001", Colours = "Red,Blue,Green", Sizes = "S,M,L,XL", Price = 1500, CreationFlow = "Dying → Handwork → Stitching" },
            new Design { DesignNumber = "AF-002", Colours = "Yellow,Pink", Sizes = "M,L,XL", Price = 2000, CreationFlow = "Dying → Handwork → Stitching" },
            new Design { DesignNumber = "AF-003", Colours = "Red,Black,White", Sizes = "S,M,L", Price = 1800, CreationFlow = "Dying → Handwork → Stitching" }
        );
        db.SaveChanges();
    }

    // Seed default standard product categories
    if (!db.ProductCategories.Any())
    {
        var kurtis = new ProductCategory { CategoryName = "Kurtis", CategoryCode = "KRT", DefaultHsnCode = "6204", DefaultGstRate = 5.0m, Description = "Designer and daily wear kurtis" };
        var sarees = new ProductCategory { CategoryName = "Sarees", CategoryCode = "SAR", DefaultHsnCode = "5208", DefaultGstRate = 5.0m, Description = "Traditional, party wear, and printed sarees" };
        var lehengas = new ProductCategory { CategoryName = "Lehengas", CategoryCode = "LHG", DefaultHsnCode = "6204", DefaultGstRate = 12.0m, Description = "Bridal and festive lehenga choli" };
        var gowns = new ProductCategory { CategoryName = "Gowns & Indo-Western", CategoryCode = "GWN", DefaultHsnCode = "6204", DefaultGstRate = 12.0m, Description = "Evening gowns and western fusion" };
        var dressMaterial = new ProductCategory { CategoryName = "Dress Material & Fabrics", CategoryCode = "DRS", DefaultHsnCode = "5208", DefaultGstRate = 5.0m, Description = "Unstitched and semi-stitched suit sets" };
        var westernWear = new ProductCategory { CategoryName = "Western Tops & Co-ords", CategoryCode = "WST", DefaultHsnCode = "6206", DefaultGstRate = 5.0m, Description = "Casual tops, shirts, and coordinate sets" };

        db.ProductCategories.AddRange(kurtis, sarees, lehengas, gowns, dressMaterial, westernWear);
        db.SaveChanges();
    }

    // Link any Designs without CategoryId based on their Category string
    var unlinkedDesigns = db.Designs.Where(d => d.CategoryId == null && !string.IsNullOrEmpty(d.Category)).ToList();
    if (unlinkedDesigns.Any())
    {
        foreach (var d in unlinkedDesigns)
        {
            var cat = db.ProductCategories.FirstOrDefault(c => c.CategoryName.ToLower() == d.Category!.ToLower());
            if (cat == null)
            {
                cat = new ProductCategory { CategoryName = d.Category!, DefaultHsnCode = d.HsnSacCode ?? "6204", DefaultGstRate = 5.0m };
                db.ProductCategories.Add(cat);
                db.SaveChanges();
            }
            d.CategoryId = cat.Id;
        }
        db.SaveChanges();
    }

    // Seed default standard pricelists (Odoo-compliant)
    if (!db.Pricelists.Any())
    {
        var publicPl = new Pricelist
        {
            Name = "Public Pricelist (INR)",
            Currency = "INR (₹)",
            DiscountPolicy = PricelistDiscountPolicy.DiscountIncluded,
            IsActive = true,
            Description = "Standard public retail pricelist with default catalogue rates.",
            CreatedDate = DateTime.Now
        };
        publicPl.Items.Add(new PricelistItem
        {
            AppliedOn = PricelistAppliedOn.AllProducts,
            ComputationMethod = PricelistComputeMethod.Percentage,
            DiscountPercentage = 0m,
            MinQuantity = 1m
        });

        var wholesalePl = new Pricelist
        {
            Name = "Wholesale Pricelist (10% Off)",
            Currency = "INR (₹)",
            DiscountPolicy = PricelistDiscountPolicy.ShowDiscount,
            IsActive = true,
            Description = "Standard wholesale B2B pricelist offering 10% discount on all garment collections.",
            CreatedDate = DateTime.Now
        };
        wholesalePl.Items.Add(new PricelistItem
        {
            AppliedOn = PricelistAppliedOn.AllProducts,
            ComputationMethod = PricelistComputeMethod.Percentage,
            DiscountPercentage = 10.0m,
            MinQuantity = 1m
        });

        var bulkTierPl = new Pricelist
        {
            Name = "Bulk Volume Tier (15% Off for 10+ Pcs)",
            Currency = "INR (₹)",
            DiscountPolicy = PricelistDiscountPolicy.DiscountIncluded,
            IsActive = true,
            Description = "Tiered volume buyer pricelist: 5% baseline, 15% discount for 10+ pieces.",
            CreatedDate = DateTime.Now
        };
        bulkTierPl.Items.Add(new PricelistItem
        {
            AppliedOn = PricelistAppliedOn.AllProducts,
            ComputationMethod = PricelistComputeMethod.Percentage,
            DiscountPercentage = 5.0m,
            MinQuantity = 1m
        });
        bulkTierPl.Items.Add(new PricelistItem
        {
            AppliedOn = PricelistAppliedOn.AllProducts,
            ComputationMethod = PricelistComputeMethod.Percentage,
            DiscountPercentage = 15.0m,
            MinQuantity = 10m
        });

        db.Pricelists.AddRange(publicPl, wholesalePl, bulkTierPl);
        db.SaveChanges();
    }

    // Seed production orders
    if (!db.ProductionOrders.Any())
    {
        var design1 = db.Designs.First(d => d.DesignNumber == "AF-001");
        var design2 = db.Designs.First(d => d.DesignNumber == "AF-002");
        var design3 = db.Designs.First(d => d.DesignNumber == "AF-003");

        db.ProductionOrders.AddRange(
            new ProductionOrder { DesignId = design1.Id, LotNo = "LOT-001", TotalQuantity = 50, Status = OrderStatus.AtStitching, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = true, IsStitchingVerified = false },
            new ProductionOrder { DesignId = design2.Id, LotNo = "LOT-002", TotalQuantity = 30, Status = OrderStatus.AtHandwork, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = false, IsStitchingVerified = false },
            new ProductionOrder { DesignId = design3.Id, LotNo = "LOT-003", TotalQuantity = 75, Status = OrderStatus.ReadyToDispatch, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = true, IsStitchingVerified = true }
        );
        db.SaveChanges();

        // Seed order details
        var order1 = db.ProductionOrders.First();
        db.ProductionOrderDetails.AddRange(
            new ProductionOrderDetail { ProductionOrderId = order1.Id, Colour = "Red", Size = "S", Quantity = 10 },
            new ProductionOrderDetail { ProductionOrderId = order1.Id, Colour = "Red", Size = "M", Quantity = 15 },
            new ProductionOrderDetail { ProductionOrderId = order1.Id, Colour = "Blue", Size = "L", Quantity = 12 },
            new ProductionOrderDetail { ProductionOrderId = order1.Id, Colour = "Green", Size = "XL", Quantity = 13 }
        );
        db.SaveChanges();
    }

    // Seed sample employees
    if (!db.Employees.Any())
    {
        var emp1 = new Employee
        {
            EmployeeCode = "EMP-001",
            FullName = "Ramesh Sharma",
            Department = "Cutting",
            Designation = "Master Cutter",
            ContactNumber = "9825100001",
            Email = "ramesh@aashana.local",
            JoiningDate = new DateTime(2025, 1, 10),
            SalaryType = SalaryType.DailyWage,
            BaseRate = 750m,
            StandardDailyHours = 8m,
            OvertimeHourlyRate = 140m,
            UpiId = "ramesh@upi",
            IsActive = true
        };

        var emp2 = new Employee
        {
            EmployeeCode = "EMP-002",
            FullName = "Priya Patel",
            Department = "Stitching",
            Designation = "Senior Stitcher",
            ContactNumber = "9825100002",
            Email = "priya@aashana.local",
            JoiningDate = new DateTime(2025, 2, 15),
            SalaryType = SalaryType.DailyWage,
            BaseRate = 650m,
            StandardDailyHours = 8m,
            OvertimeHourlyRate = 125m,
            BankName = "State Bank of India",
            BankAccountNumber = "30291823719",
            BankIFSC = "SBIN0001234",
            IsActive = true
        };

        var emp3 = new Employee
        {
            EmployeeCode = "EMP-003",
            FullName = "Abdul Khan",
            Department = "Quality",
            Designation = "Floor Quality Inspector",
            ContactNumber = "9825100003",
            Email = "abdul@aashana.local",
            JoiningDate = new DateTime(2024, 11, 1),
            SalaryType = SalaryType.MonthlySalary,
            BaseRate = 22000m,
            StandardDailyHours = 8m,
            OvertimeHourlyRate = 135m,
            UpiId = "abdul@paytm",
            IsActive = true
        };

        var emp4 = new Employee
        {
            EmployeeCode = "EMP-004",
            FullName = "Sunita Verma",
            Department = "Finishing",
            Designation = "Finishing & Ironing",
            ContactNumber = "9825100004",
            Email = "sunita@aashana.local",
            JoiningDate = new DateTime(2025, 4, 1),
            SalaryType = SalaryType.HourlyRate,
            BaseRate = 95m,
            StandardDailyHours = 8m,
            OvertimeHourlyRate = 145m,
            IsActive = true
        };

        db.Employees.AddRange(emp1, emp2, emp3, emp4);
        db.SaveChanges();

        // Seed sample attendance for today and recent days
        var today = DateTime.Today;
        db.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                EmployeeId = emp1.Id,
                Date = today,
                CheckInTime = today.AddHours(9).AddMinutes(5),
                CheckOutTime = null,
                TotalHours = 0,
                OvertimeHours = 0,
                Status = AttendanceStatus.Present,
                VerificationMethod = VerificationMethod.FaceScan,
                FaceConfidence = 96.4
            },
            new AttendanceRecord
            {
                EmployeeId = emp2.Id,
                Date = today,
                CheckInTime = today.AddHours(9).AddMinutes(42),
                CheckOutTime = null,
                TotalHours = 0,
                OvertimeHours = 0,
                Status = AttendanceStatus.Late,
                VerificationMethod = VerificationMethod.FaceScan,
                FaceConfidence = 94.2,
                Notes = "Late check-in at 09:42 AM"
            },
            new AttendanceRecord
            {
                EmployeeId = emp1.Id,
                Date = today.AddDays(-1),
                CheckInTime = today.AddDays(-1).AddHours(9).AddMinutes(2),
                CheckOutTime = today.AddDays(-1).AddHours(18).AddMinutes(35),
                TotalHours = 9.5m,
                OvertimeHours = 1.5m,
                Status = AttendanceStatus.Present,
                VerificationMethod = VerificationMethod.FaceScan,
                FaceConfidence = 97.1
            },
            new AttendanceRecord
            {
                EmployeeId = emp2.Id,
                Date = today.AddDays(-1),
                CheckInTime = today.AddDays(-1).AddHours(9).AddMinutes(0),
                CheckOutTime = today.AddDays(-1).AddHours(17).AddMinutes(30),
                TotalHours = 8.5m,
                OvertimeHours = 0.5m,
                Status = AttendanceStatus.Present,
                VerificationMethod = VerificationMethod.FaceScan,
                FaceConfidence = 95.8
            },
            new AttendanceRecord
            {
                EmployeeId = emp3.Id,
                Date = today.AddDays(-1),
                CheckInTime = today.AddDays(-1).AddHours(9).AddMinutes(15),
                CheckOutTime = today.AddDays(-1).AddHours(18).AddMinutes(0),
                TotalHours = 8.75m,
                OvertimeHours = 0.75m,
                Status = AttendanceStatus.Present,
                VerificationMethod = VerificationMethod.ManualPunch
            }
        );
        db.SaveChanges();
    }

    // Seed sample physical biometric hardware terminal
    if (!db.BiometricDevices.Any())
    {
        db.BiometricDevices.Add(new BiometricDevice
        {
            DeviceName = "Main Factory Gate Face Scanner",
            DeviceIdentifier = "SN-AF-FACE-01",
            DeviceModel = "eSSL / ZKTeco Face Recognition Terminal",
            Location = "Factory Main Entrance Gate",
            IpAddress = "192.168.1.200",
            ApiKey = "af_face_terminal_key_01",
            LastHeartbeat = DateTime.Now.AddMinutes(-5),
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }
}

#if DEBUG
if (args.Contains("--verify-all") || args.Contains("--test-all"))
{
    var passed = await DataVerificationRunner.RunAllModuleTestsAsync(app.Services);
    Environment.Exit(passed ? 0 : 1);
}
#endif

app.Run();

