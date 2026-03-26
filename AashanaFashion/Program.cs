using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var activeConnection = builder.Configuration.GetValue<string>("ActiveConnection") ?? "DefaultConnection";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(activeConnection)));

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
}

app.Run();
