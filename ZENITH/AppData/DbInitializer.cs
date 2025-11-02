using Microsoft.AspNetCore.Identity;
using ZENITH.AppData;
using ZENITH.Models;

using ZENITH.AppData;
using ZENITH.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class DbInitializer
{
    // Thêm serviceProvider để có thể sử dụng UserManager và RoleManager
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // 1. KIỂM TRA: Đã có vai trò nào chưa
        if (roleManager.Roles.Any())
        {
            return; // CSDL đã có dữ liệu, thoát
        }

        // --- 2. TẠO CÁC VAI TRÒ (ROLES) ---
        // Vai trò Admin (phải tạo trước)
        await roleManager.CreateAsync(new ApplicationRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            Description = "Quản trị viên hệ thống"
        });

        // Vai trò Customer
        await roleManager.CreateAsync(new ApplicationRole
        {
            Name = "Customer",
            NormalizedName = "CUSTOMER",
            Description = "Khách hàng mua sắm"
        });

        // --- 3. TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH ---
        var adminUser = new ApplicationUser
        {
            UserName = "admin@zenith.com",
            Email = "admin@zenith.com",
            FullName = "Admin ZENITH",
            EmailConfirmed = true,
            CreatedAt = DateTime.Now
            // Các trường khác được Identity quản lý
        };

        // Tạo người dùng Admin với mật khẩu
        // Đặt mật khẩu đơn giản cho môi trường dev, nhưng đảm bảo phức tạp khi triển khai thực tế!
        var result = await userManager.CreateAsync(adminUser, "Admin@123");

        if (result.Succeeded)
        {
            // Gán vai trò "Admin" cho tài khoản vừa tạo
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // --- 4. SEED DỮ LIỆU BÁN HÀNG (OPTIONAL) ---
        // Logic thêm Products, Categories của bạn sẽ nằm ở đây
        // ... (Bạn có thể thêm code Seeding Products/Categories từ phản hồi trước vào đây)
    }
}