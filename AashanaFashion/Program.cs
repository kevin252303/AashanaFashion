using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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

    // Seed users
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new AppUser { Username = "admin",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),   Role = "Admin",   FullName = "Admin User",   IsActive = true },
            new AppUser { Username = "manager", PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"), Role = "Manager", FullName = "Production Manager", IsActive = true },
            new AppUser { Username = "viewer",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("viewer123"),  Role = "Viewer",  FullName = "Floor Viewer", IsActive = true }
        );
        db.SaveChanges();
    }

    // Seed production orders
    if (!db.ProductionOrders.Any())
    {
        db.ProductionOrders.AddRange(
            new ProductionOrder { DesignNumber = "AF-001", FabricType = "Silk",      TotalQuantity = 50,  Status = OrderStatus.AtStitching,      IsRawMaterialVerified = true,  IsDyingVerified = true,  IsHandworkVerified = true,  IsStitchingVerified = false },
            new ProductionOrder { DesignNumber = "AF-002", FabricType = "Cotton",    TotalQuantity = 30,  Status = OrderStatus.AtHandwork,        IsRawMaterialVerified = true,  IsDyingVerified = true,  IsHandworkVerified = false, IsStitchingVerified = false },
            new ProductionOrder { DesignNumber = "AF-003", FabricType = "Chiffon",   TotalQuantity = 75,  Status = OrderStatus.ReadyToDispatch,   IsRawMaterialVerified = true,  IsDyingVerified = true,  IsHandworkVerified = true,  IsStitchingVerified = true  },
            new ProductionOrder { DesignNumber = "AF-004", FabricType = "Lawn",      TotalQuantity = 100, Status = OrderStatus.Dispatched,        IsRawMaterialVerified = true,  IsDyingVerified = true,  IsHandworkVerified = true,  IsStitchingVerified = true  },
            new ProductionOrder { DesignNumber = "AF-005", FabricType = "Georgette", TotalQuantity = 20,  Status = OrderStatus.AtDying,           IsRawMaterialVerified = true,  IsDyingVerified = false, IsHandworkVerified = false, IsStitchingVerified = false }
        );
        db.SaveChanges();
    }
}

app.Run();
