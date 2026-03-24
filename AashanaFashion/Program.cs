using AashanaFashion.Data;
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
        options.AccessDeniedPath = "/Account/Login";
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
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed sample data if DB is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AashanaFashion.Data.AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.ProductionOrders.Any())
    {
        db.ProductionOrders.AddRange(
            new AashanaFashion.Models.ProductionOrder { DesignNumber = "AF-001", FabricType = "Silk", TotalQuantity = 50, Status = AashanaFashion.Models.OrderStatus.AtStitching, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = true, IsStitchingVerified = false },
            new AashanaFashion.Models.ProductionOrder { DesignNumber = "AF-002", FabricType = "Cotton", TotalQuantity = 30, Status = AashanaFashion.Models.OrderStatus.AtHandwork, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = false, IsStitchingVerified = false },
            new AashanaFashion.Models.ProductionOrder { DesignNumber = "AF-003", FabricType = "Chiffon", TotalQuantity = 75, Status = AashanaFashion.Models.OrderStatus.ReadyToDispatch, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = true, IsStitchingVerified = true },
            new AashanaFashion.Models.ProductionOrder { DesignNumber = "AF-004", FabricType = "Lawn", TotalQuantity = 100, Status = AashanaFashion.Models.OrderStatus.Dispatched, IsRawMaterialVerified = true, IsDyingVerified = true, IsHandworkVerified = true, IsStitchingVerified = true },
            new AashanaFashion.Models.ProductionOrder { DesignNumber = "AF-005", FabricType = "Georgette", TotalQuantity = 20, Status = AashanaFashion.Models.OrderStatus.AtDying, IsRawMaterialVerified = true, IsDyingVerified = false, IsHandworkVerified = false, IsStitchingVerified = false }
        );
        db.SaveChanges();
    }
}

app.Run();
