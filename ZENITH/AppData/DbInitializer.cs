using ZENITH.AppData;
using ZENITH.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class DbInitializer
{
    private static int sportDisplayOrder = 1;
    private static int categoryDisplayOrder = 1;
    
    // Dictionary cho Sport Icon (Sử dụng ký hiệu ~/ dẫn đến wwwroot)
    private static Dictionary<string, string> SportIcons = new Dictionary<string, string>()
    {
        { "Leo Núi & Dã Ngoại", "~/image/categoryImage/sport/hiking-trekking.svg" },
        { "Chạy Bộ & Đi Bộ", "~/image/categoryImage/sport/chay-bo-duong-truong.svg" },
        { "Yoga & Pilates", "~/image/categoryImage/sport/quan-ao-yoga.svg" },
        { "Bơi Lội", "~/image/categoryImage/sport/do-boi-loi.svg" },
        { "Võ Thuật Tổng Hợp", "~/image/categoryImage/sport/boxing.svg" }, 
        { "Đạp Xe", "~/image/categoryImage/sport/xe-dap.svg" },
        { "Thể Thao Dùng Vợt", "~/image/categoryImage/sport/cau-long.svg" },
        { "Thể Thao Đồng Đội", "~/image/categoryImage/sport/football.svg" },

        // Sport Con Icons 
        { "Hiking & Trekking", "~/image/categoryImage/sport/hiking-trekking.svg" },
        { "Cắm Trại", "~/image/categoryImage/sport/camtrai.svg" },
        { "Leo Núi Nhân Tạo", "~/image/categoryImage/sport/leo-nui-nhan-tao.svg" },
        { "Chạy Bộ Đường Trường", "~/image/categoryImage/sport/chay-bo-duong-truong.svg" },
        { "Chạy Địa Hình (Trail)", "~/image/categoryImage/sport/trail.svg" },
        { "Đi Bộ", "~/image/categoryImage/sport/di-bo.svg" },
        { "Quần Áo Yoga & Pilates", "~/image/categoryImage/sport/quan-ao-yoga.svg" },
        { "Thảm yoga pilates & túi đựng thảm", "~/image/categoryImage/sport/tham-tui-yoga.svg" },
        { "Đồ bơi", "~/image/categoryImage/sport/do-boi-loi.svg" },
        { "Kính bơi", "~/image/categoryImage/sport/kinh-mu-boi-loi.svg" },
        { "Mũ Bơi", "~/image/categoryImage/sport/kinh-mu-boi-loi.svg" },
        { "Phụ Kiện bơi", "~/image/categoryImage/sport/phu-kien-boi.svg" },
        { "Xe đạp", "~/image/categoryImage/sport/xe-dap.svg" },
        { "Phụ kiện xe đạp", "~/image/categoryImage/sport/phu-kien-xe-dap.svg" },
        { "Quần Áo Đạp Xe", "~/image/categoryImage/sport/quan-ao-xe-dap.svg" },
        { "Phụ Tùng & Bảo Dưỡng", "~/image/categoryImage/sport/phu-tung-bao-duong-xe-dap.svg" },
        { "Boxing & Muay Thai", "~/image/categoryImage/sport/boxing.svg" },
        { "Cầu Lông", "~/image/categoryImage/sport/cau-long.svg" },
        { "TENNIS", "~/image/categoryImage/sport/tennis.svg" },
        { "Bóng Bàn", "~/image/categoryImage/sport/bong-ban.svg" },
        { "Pickleball", "~/image/categoryImage/sport/pickleball.svg" },
        { "Bóng Đá & Futsal", "~/image/categoryImage/sport/football.svg" },
        { "Bóng rổ", "~/image/categoryImage/sport/bong-ro.svg" },
        { "Bóng chuyền", "~/image/categoryImage/sport/bong-chuyen.svg" },
        { "Bóng chày", "~/image/categoryImage/sport/bong-chay.svg" },
    };
    
    // Mô tả chi tiết cho Categories (để tránh gán Description = CategoryName)
    private static Dictionary<string, string> CategoryDescriptions = new Dictionary<string, string>()
    {
        { "Balo & Túi", "Tất cả các loại balo, túi chuyên dụng và túi đựng đồ cá nhân cho các chuyến đi." },
        { "Quần Áo Leo Núi", "Trang phục bền bỉ, thoáng khí và chống thấm nước cho hoạt động leo núi và thám hiểm." },
        { "Giày Leo Núi", "Giày chuyên dụng có độ bám cao và hỗ trợ mắt cá chân, chống trượt trên địa hình hiểm trở." },
        { "Giày Chạy Bộ", "Các mẫu giày chạy bộ đệm êm, nhẹ và có độ đàn hồi cao, phù hợp cho đường nhựa." },
        { "Găng Tay Boxing", "Găng tay bảo hộ và găng tay tập luyện với nhiều trọng lượng khác nhau." },
        { "Xe Đạp Đường Trường", "Xe đạp thiết kế tối ưu tốc độ, khung nhẹ, và lốp mỏng cho đường bằng phẳng." },
        { "Vợt cầu lông & quả cầu lông", "Các loại vợt carbon, hợp kim và quả cầu lông tiêu chuẩn thi đấu." },
        { "Quả Bóng Đá", "Bóng đá tiêu chuẩn thi đấu và tập luyện, chịu được mọi điều kiện sân cỏ." },
        { "Kính Bơi Người Lớn", "Kính bơi chống sương mù, chống tia UV cho người lớn và vận động viên." },
        { "Kính Bơi Cận", "Kính bơi có thấu kính điều chỉnh độ cận, giúp nhìn rõ dưới nước." },
        // Sử dụng mô tả mặc định cho các mục còn lại.
    };


    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await context.Database.MigrateAsync();

        var now = DateTime.Now;

        // ====================================================================
        // 2. IDENTITY SEEDING (ROLES & ADMIN)
        // ====================================================================

        // --- TẠO ROLES ---
        var roles = new ApplicationRole[]
        {
            new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN", Description = "Quản trị viên hệ thống" },
            new ApplicationRole { Name = "Customer", NormalizedName = "CUSTOMER", Description = "Khách hàng mua sắm" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }

        // --- TẠO TÀI KHOẢN ADMIN ---
        const string adminEmail = "admin@zenith.com";
        const string adminPass = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin ZENITH",
                EmailConfirmed = true,
                CreatedAt = now
            };
            var result = await userManager.CreateAsync(adminUser, adminPass);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // ====================================================================
        // 3. SPORTS, CATEGORIES & LINKING SEEDING (IDEMPOTENT)
        // ====================================================================
        {
            // Mảng dữ liệu SPORTS/CATEGORIES của bạn
            var sportData = new (string ParentName, string SportName, string[] Categories)[]
            {
                // --- LEO NÚI & DÃ NGOẠI ---
                (null, "Leo Núi & Dã Ngoại", Array.Empty<string>()),
                ("Leo Núi & Dã Ngoại", "Hiking & Trekking", new[] { "Balo & Túi", "Quần Áo Leo Núi", "Giày Leo Núi", "Phụ Kiện Leo Núi", "Vớ Leo Núi" }),
                ("Leo Núi & Dã Ngoại", "Cắm Trại", new[] { "Lều Cắm Trại", "Túi Ngủ & Đệm Hơi", "Ghế Cắm Trại", "Bàn Cắm Trại", "Dụng Cụ Nấu Ăn", "Đèn Cắm Trại", "Vệ Sinh Cá Nhân" }),
                ("Leo Núi & Dã Ngoại", "Leo Núi Nhân Tạo", new[] { "Thiết Bị Leo Núi Nhân Tạo", "Giày & Mũ Bảo Hiểm Leo Núi Nhân Tạo", "Phụ Kiện Leo Núi Nhân Tạo" }),
                
                // --- CHẠY BỘ & ĐI BỘ ---
                (null, "Chạy Bộ & Đi Bộ", Array.Empty<string>()),
                ("Chạy Bộ & Đi Bộ", "Chạy Bộ Đường Trường", new[] { "Giày Chạy Bộ", "Quần Áo Chạy Bộ Nam", "Quần Áo Chạy Bộ Nữ", "Phụ Kiện Chạy Bộ", "Chạy Bộ Mùa Lạnh", "Dinh Dưỡng, Băng Gối & Dụng Cụ Massage" }),
                ("Chạy Bộ & Đi Bộ", "Chạy Địa Hình (Trail)", new[] { "Quần Áo Chạy Trail", "Giày Chạy Trail", "Túi Nước & Đai Chạy Địa Hình", "Phụ Kiện Chạy Trail" }),
                ("Chạy Bộ & Đi Bộ", "Đi Bộ", new[] { "Giày Đi Bộ", "Phụ Kiện Đi Bộ", "Tất Đi Bộ" }),

                // --- YOGA & PILATES ---
                (null, "Yoga & Pilates", Array.Empty<string>()),
                ("Yoga & Pilates", "Quần Áo Yoga & Pilates", new[] { "Áo Tập Yoga", "Áo Ngực Tập Yoga", "Quần Tập Yoga", "Quần Legging Yoga", "Tất Chống Trượt", "Đồ tập yoga nam", "Đồ tập yoga nữ" }),
                ("Yoga & Pilates", "Thảm yoga pilates & túi đựng thảm", new[] { "Thảm yoga & Pilates", "Túi đựng thảm yoga" }),

                // --- BƠI LỘI ---
                (null, "Bơi Lội", Array.Empty<string>()),
                ("Bơi Lội", "Đồ bơi", new[] { "Đồ bơi nữ", "Đồ bơi nam" }),
                ("Bơi Lội", "Kính bơi", new[] { "Kính Bơi Người Lớn", "Kính Bơi Cận" }),
                ("Bơi Lội", "Mũ Bơi", new[] { "Mũ Bơi Người Lớn" }),
                ("Bơi Lội", "Phụ Kiện bơi", new[] { "Phao Bơi", "Khăn tắm", "Chân vịt bơi", "Kẹp Mũi & Bịt Tai", "Sữa Tắm & Kem Chống Nắng", "Bể Bơi Tại Nhà" }),
                
                // --- VÕ THUẬT TỔNG HỢP ---
                (null, "Võ Thuật Tổng Hợp", Array.Empty<string>()),
                ("Võ Thuật Tổng Hợp", "Boxing & Muay Thai", new[] { "Găng Tay Boxing", "Túi Cát & Đệm Boxing", "Bảo Vệ Hàm Boxing", "Dây Quấn Tay", "Đồ Bảo Vệ", "Quần Áo & Giày Boxing", "Phụ Kiện" }),
                
                // --- ĐẠP XE ---
                (null, "Đạp Xe", Array.Empty<string>()),
                ("Đạp Xe", "Xe đạp", new[] { "Xe Đạp Đường Trường", "Xe Đạp Địa Hình & Hybrid", "Xe Đạp Gấp", "Xe Đạp Thành Phố" }),
                ("Đạp Xe", "Phụ kiện xe đạp", new[] { "Đèn Xe Đạp", "Khóa xe đạp", "Túi xe đạp", "Giỏ xe đạp", "Chuông xe đạp", "Ba-ga xe đạp", "Yên xe đạp" }),
                ("Đạp Xe", "Quần Áo Đạp Xe", new[] { "Quần Đạp Xe", "Áo Đạp Xe", "Áo khoác đạp xe" }),
                ("Đạp Xe", "Phụ Tùng & Bảo Dưỡng", new[] { "Lốp Xe", "Săm Xe", "Linh Kiện Xe Đạp", "Phanh Xe", "Tay Lái & Pô-tăng", "Bàn Đạp", "Dụng Cụ Bảo Dưỡng" }),

                // --- THỂ THAO DÙNG VỢT ---
                (null, "Thể Thao Dùng Vợt", Array.Empty<string>()),
                ("Thể Thao Dùng Vợt", "Cầu Lông", new[] { "Vợt cầu lông & quả cầu lông", "Giày & tất cầu lông", "Quần áo cầu lông", "Phụ kiện cầu lông", "Lưới cầu lông" }),
                ("Thể Thao Dùng Vợt", "TENNIS", new[] { "Vợt Tennis & Bóng", "Trang phục tennis", "Giày & Tất Tennis", "Phụ kiện Tennis" }),
                ("Thể Thao Dùng Vợt", "Bóng Bàn", new[] { "Vợt bóng bàn", "Bàn & Lưới Bóng Bàn", "Trang phục bóng bàn", "Bao vợt" }),
                ("Thể Thao Dùng Vợt", "Pickleball", new[] { "Vợt pickleball", "Quần áo & phụ kiện Pickleball", "Phụ kiện pickleball" }),
                
                // --- THỂ THAO ĐỒNG ĐỘI ---
                (null, "Thể Thao Đồng Đội", Array.Empty<string>()),
                ("Thể Thao Đồng Đội", "Bóng Đá & Futsal", new[] { "Giày Bóng Đá & Futsal", "Quả Bóng Đá", "Quần Áo Bóng Đá", "Thiết Bị & Phụ Kiện Bóng Đá" }),
                ("Thể Thao Đồng Đội", "Bóng rổ", new[] { "Quả bóng rổ", "Giày bóng rổ", "Quần Áo bóng rổ", "Dụng cụ & Phụ Kiện Bóng rổ" }),
                ("Thể Thao Đồng Đội", "Bóng chuyền", new[] { "Bóng, Lưới & Dụng Cụ", "Quần Áo Bóng Chuyền", "Phụ Kiện Bóng Chuyền" }),
                ("Thể Thao Đồng Đội", "Bóng chày", new[] { "Gậy & Quả bóng chày", "Găng tay bóng chày" }),
            };

            // Dùng Dictionary để lưu các Category mới được tạo
            var seededCategories = new Dictionary<string, Category>();
            
            // --- 3.1 TẠO/ĐẢM BẢO CATEGORY (CÓ ImageUrl và Description) ---
            var existingCategories = await context.Categories
                .ToDictionaryAsync(c => c.CategoryName, c => c);

            int categoryImageIndex = 1;

            foreach (var item in sportData)
            {
                foreach (var categoryName in item.Categories)
                {
                    // TÌM DESCRIPTION TỪ DICTIONARY HOẶC DÙNG MẶC ĐỊNH
                    CategoryDescriptions.TryGetValue(categoryName, out var description);
                    
                    if (!existingCategories.ContainsKey(categoryName))
                    {
                        // LOGIC TẠO ImageUrl: category-{index}.avif
                        var imageUrl = $"~/image/categoryImage/category/category-{categoryImageIndex}.avif";
                        
                        var category = new Category
                        {
                            CategoryName = categoryName,
                            Description = description ?? $"Danh mục sản phẩm {categoryName} cho môn thể thao hiện đại.", 
                            ImageUrl = imageUrl, 
                            IsActive = true,
                            DisplayOrder = categoryDisplayOrder++,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        context.Categories.Add(category);
                        existingCategories[categoryName] = category;
                        
                        categoryImageIndex++; 
                    }
                    else
                    {
                         // Cập nhật Description và ImageUrl nếu Category đã tồn tại
                         var existingCategory = existingCategories[categoryName];
                         var imageUrl = $"~/image/categoryImage/category/category-{categoryImageIndex}.avif";

                         if (existingCategory.Description != description)
                         {
                             existingCategory.Description = description ?? $"Danh mục sản phẩm {categoryName} cho môn thể thao hiện đại.";
                             context.Categories.Update(existingCategory);
                         }
                         if (existingCategory.ImageUrl != imageUrl)
                         {
                             existingCategory.ImageUrl = imageUrl;
                             context.Categories.Update(existingCategory);
                         }
                         
                         categoryImageIndex++;
                    }
                }
            }
            await context.SaveChangesAsync();

            // --- 3.2 TẠO/ĐẢM BẢO SPORTS (CÓ IconUrl) ---
            var existingSports = await context.Sports
                .Include(s => s.ParentSport)
                .ToDictionaryAsync(s => s.SportName, s => s);

            foreach (var item in sportData)
            {
                Sport? parent = null;
                if (!string.IsNullOrEmpty(item.ParentName))
                {
                    existingSports.TryGetValue(item.ParentName!, out parent);
                    if (parent == null)
                    {
                        SportIcons.TryGetValue(item.ParentName!, out var parentIconUrl); 
                        parent = new Sport
                        {
                            SportName = item.ParentName!,
                            Description = item.ParentName!,
                            IconUrl = parentIconUrl, 
                            IsActive = true,
                            DisplayOrder = sportDisplayOrder++,
                            CreatedAt = now
                        };
                        context.Sports.Add(parent);
                        await context.SaveChangesAsync();
                        existingSports[item.ParentName!] = parent;
                    }
                }
                
                // Lấy IconUrl cho Sport hiện tại
                SportIcons.TryGetValue(item.SportName, out var iconUrl);

                if (!existingSports.ContainsKey(item.SportName))
                {
                    
                    var sport = new Sport
                    {
                        SportName = item.SportName,
                        Description = item.SportName,
                        ParentSport = parent,
                        IconUrl = iconUrl, 
                        IsActive = true,
                        DisplayOrder = sportDisplayOrder++,
                        CreatedAt = now
                    };
                    context.Sports.Add(sport);
                    await context.SaveChangesAsync();
                    existingSports[item.SportName] = sport;
                }
                else if (existingSports.TryGetValue(item.SportName, out var existingSport))
                {
                    // CẬP NHẬT IconUrl nếu Sports đã tồn tại
                    if (existingSport.IconUrl != iconUrl)
                    {
                        existingSport.IconUrl = iconUrl;
                        context.Sports.Update(existingSport); 
                    }
                }
            }
            await context.SaveChangesAsync(); // Lưu thay đổi IconUrl (nếu có)


            // --- 3.3 TẠO/ĐẢM BẢO LIÊN KẾT SPORT-CATEGORY ---
            var existingLinks = new HashSet<(int SportId, int CategoryId)>(
                (await context.SportCategories
                    .AsNoTracking()
                    .Select(sc => new { sc.SportId, sc.CategoryId })
                    .ToListAsync())
                .Select(x => (x.SportId, x.CategoryId))
            );

            foreach (var item in sportData)
            {
                var sport = existingSports[item.SportName];
                foreach (var categoryName in item.Categories)
                {
                    var category = existingCategories[categoryName];
                    var key = (sport.SportId, category.CategoryId);
                    if (!existingLinks.Contains(key))
                    {
                        context.SportCategories.Add(new SportCategory
                        {
                            SportId = sport.SportId,
                            CategoryId = category.CategoryId
                        });
                        existingLinks.Add(key);
                    }
                }
            }
            await context.SaveChangesAsync();
        }
    }
}