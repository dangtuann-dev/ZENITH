using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZENITH.AppData;
using Microsoft.AspNetCore.Identity;
using ZENITH.Models;
using ZENITH.Services;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.AspNetCore.Identity.UI.Services;
var builder = WebApplication.CreateBuilder(args);

var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var contentRoot = builder.Environment.ContentRootPath;
var dbFile = Path.Combine(contentRoot, "AppData", "ZenithDB.mdf");
var dbDir = Path.GetDirectoryName(dbFile);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
var connectionString = rawConnectionString;
if (rawConnectionString.Contains("(localdb)\\mssqllocaldb", StringComparison.OrdinalIgnoreCase))
{
    var dbName = Path.GetFileNameWithoutExtension(dbFile) + "_AppData";
    var csb = new SqlConnectionStringBuilder(rawConnectionString)
    {
        AttachDBFilename = dbFile,
        InitialCatalog = dbName
    };
    connectionString = csb.ConnectionString;
}

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Removed Google authentication
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Cấu hình các tùy chọn bảo mật
    options.SignIn.RequireConfirmedAccount = false; // Tạm tắt yêu cầu xác nhận email cho dev
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<ApplicationRole>() // Sử dụng ApplicationRole tùy chỉnh
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapStaticAssets();
app.UseStaticFiles();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

async Task SeedDatabase(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.Initialize(scope.ServiceProvider);
    }
}

// Gọi hàm SeedDatabase ngay trước app.Run()
await SeedDatabase(app);

app.Run();
