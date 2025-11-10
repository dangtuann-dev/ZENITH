using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.ViewModels;
using ZENITH.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace ZENITH.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) // DI hoạt động là CSDL được tìm thấy
        {
            _context = context;
        }
        private IQueryable<Product> GetBaseProductQuery()
        {
            // Hàm trợ giúp để tạo truy vấn cơ bản (Tránh lặp lại Include)
            return _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages.Where(i => i.IsPrimary))
                .Include(p => p.Category) // Cần Include Category để lọc
                .Where(p => p.IsActive)
                .AsNoTracking();
        }

        private List<ProductCardViewModel> MapToCardViewModel(List<Product> products)
        {
            // Hàm trợ giúp để ánh xạ kết quả truy vấn sang ViewModel
            return products.Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                SkuBase = p.Sku,
                SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                Price = p.ProductVariants.OrderByDescending(v => v.Price).FirstOrDefault()?.Price ?? 0,
                SalePrice = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.SalePrice ?? 0,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl ?? "~/image/default.webp",
                Rating = 4.5
            }).ToList();
        }
        public async Task<IActionResult> Index()
        {
            // --- 0. DỮ LIỆU CHUNG ---
            var totalCount = await _context.Products.CountAsync();
            var parentSports = await _context.Sports
    .Where(s => s.IsActive && s.ParentSportId == null)
    .OrderBy(s => s.DisplayOrder)
    .ToListAsync();
            // Lấy Category cho mục "Danh Mục Các Môn Thể Thao Phổ Biến"
            // Giả định bạn chỉ muốn Category gốc (ParentId == null)
            var parentCategories = await _context.Categories
                .Where(c => c.IsActive && c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // --- 1. SẢN PHẨM BÁN CHẠY (TOP SELLING - 15 SP) ---
            var topSellingProducts = await GetBaseProductQuery()
                .OrderByDescending(p => p.ProductVariants.Sum(v => v.SoldCount))
                .Take(15) // Lấy 15 sản phẩm
                .ToListAsync();

            // --- 2. SẢN PHẨM DÙNG VỢT (RACKET SPORTS - 20 SP) ---
            var racketSportKeywords = new[] { "Vợt cầu lông & quả cầu lông", "Vợt Tennis & Bóng", "Vợt bóng bàn", "Vợt pickleball" };
            var racketProducts = await GetBaseProductQuery()
                .Where(p => racketSportKeywords.Contains(p.Category.CategoryName))
                .OrderByDescending(p => p.CreatedAt) // Sắp xếp theo mới nhất trong nhóm này
                .Take(20)
                .ToListAsync();

            // --- 3. SẢN PHẨM LEO NÚI (CLIMBING/HIKING - 20 SP) ---
            var climbingKeywords = new[] { "Quần Áo Leo Núi", "Giày Leo Núi", "Balo & Túi" };
            var climbingProducts = await GetBaseProductQuery()
                .Where(p => climbingKeywords.Contains(p.Category.CategoryName))
                .OrderBy(p => Guid.NewGuid())
                .Take(20)
                .ToListAsync();

            // --- 4. SẢN PHẨM THỂ THAO ĐỒNG ĐỘI (TEAM SPORTS - 10 SP) ---
            var teamSportKeywords = new[] { "Bóng Đá", "Bóng Rổ", "Bóng Chuyền" };
            var teamSportsProducts = await GetBaseProductQuery()
                .Where(p => teamSportKeywords.Contains(p.Category.CategoryName))
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();
            //Giaỳ
            var footwearKeywords = new[] { "Giày Chạy Bộ", "Giày Chạy Trail", "Giày Leo Núi", "Giày Đi Bộ" };
            var footwearCollection = await GetBaseProductQuery()
                .Where(p => footwearKeywords.Contains(p.Category.CategoryName))
                .OrderByDescending(p => p.ViewCount) // Sắp xếp theo mức độ phổ biến
                .Take(15)
                .ToListAsync();


            // 5. ÁNH XẠ VÀ GÁN VÀO VIEWMODEL TỔNG HỢP
            var viewModel = new HomeIndexViewModel
            {
                TotalCount = totalCount,
                Categories = parentCategories, // Dùng cho row Danh Mục

                TopSellingProducts = MapToCardViewModel(topSellingProducts),
                RacketSportsProducts = MapToCardViewModel(racketProducts),
                ClimbingProducts = MapToCardViewModel(climbingProducts),
                TeamSportsProducts = MapToCardViewModel(teamSportsProducts)
            };
            ViewBag.ParentSports = parentSports;
            return View(viewModel);
        }




    }
}
