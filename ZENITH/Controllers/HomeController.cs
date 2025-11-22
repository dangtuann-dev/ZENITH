using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.ViewModels;
using ZENITH.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;


namespace ZENITH.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) 
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
                .Include(p => p.Reviews)
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
                // Chọn biến thể dùng để hiển thị giá bán (SalePrice) và lấy VariantId tương ứng
                Price = p.ProductVariants.OrderByDescending(v => v.Price).FirstOrDefault()?.Price ?? 0,
                SalePrice = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.SalePrice ?? 0,
                VariantId = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.VariantId ??
                            p.ProductVariants.FirstOrDefault()?.VariantId ?? 0,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl ?? "~/image/default.webp",
                Rating = p.Reviews != null && p.Reviews.Any() ? (double)p.Reviews.Average(r => r.Rating) : 0
            }).ToList();
        }
        public async Task<IActionResult> Index()
        {
            var totalCount = await _context.Products.CountAsync();
            var parentSports = await _context.Sports
    .Where(s => s.IsActive && s.ParentSportId == null)
    .OrderBy(s => s.DisplayOrder)
    .ToListAsync();
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
            var climbingKeywords = new[] { "Quần Áo Leo Núi", "Giày Leo Núi", "Balo & Túi", "Phụ Kiện Leo Núi", "Lều Cắm Trại", "Túi Ngủ & Đệm Hơi" , "Ghế Cắm Trại", "Bàn Cắm Trại", "Vệ Sinh Cá Nhân" };
            var climbingProducts = await GetBaseProductQuery()
                .Where(p => climbingKeywords.Contains(p.Category.CategoryName))
                //.OrderBy(p => Guid.NewGuid()) random
                 .OrderByDescending(p => p.ViewCount)
                .Take(20)
                .ToListAsync();

            // --- 4. SẢN PHẨM QUẦN ÁO THỂ THAO ĐỒNG ĐỘI (TEAM SPORTS - 10 SP) ---
            var teamSportKeywords = new[] { "Quần Áo Bóng Đá", "Quần Áo bóng rổ", "Quần Áo Bóng Chuyền" };
            var teamSportsProducts = await GetBaseProductQuery()
                .Where(p => teamSportKeywords.Contains(p.Category.CategoryName))
                 .OrderByDescending(p => p.ViewCount)
                .Take(20)
                .ToListAsync();
            //Giaỳ
            var footwearKeywords = new[] { "Giày Leo Núi", "Giày Chạy Bộ", "Giày Chạy Trail", "Giày Đi Bộ" , "Giày & tất cầu lông" , "Giày & Tất Tennis", "Giày Bóng Đá & Futsal", "Giày bóng rổ" };
            var footwearCollection = await GetBaseProductQuery()
                .Where(p => footwearKeywords.Contains(p.Category.CategoryName))
                .OrderByDescending(p => p.ViewCount) // Sắp xếp theo mức độ phổ biến
                .Take(20)
                .ToListAsync();


            // 5. ÁNH XẠ VÀ GÁN VÀO VIEWMODEL TỔNG HỢP
            var viewModel = new HomeIndexViewModel
            {
                TotalCount = totalCount,
                Categories = parentCategories, // Dùng cho row Danh Mục

                FootwearCollection = MapToCardViewModel(footwearCollection),
                TopSellingProducts = MapToCardViewModel(topSellingProducts),
                RacketSportsProducts = MapToCardViewModel(racketProducts),
                ClimbingProducts = MapToCardViewModel(climbingProducts),
                TeamSportsProducts = MapToCardViewModel(teamSportsProducts)
            };
            // Đặt trạng thái đã yêu thích ban đầu dựa trên danh sách yêu thích của user
            var favoriteVariantIds = new HashSet<int>();
            if (User?.Identity?.IsAuthenticated == true)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var favIds = await _context.Favorites
                        .Where(f => f.UserId == currentUserId)
                        .Select(f => f.VariantId)
                        .ToListAsync();
                    favoriteVariantIds = favIds.ToHashSet();
                }
            }

            void markFavorites(List<ProductCardViewModel> list)
            {
                if (list == null) return;
                foreach (var item in list)
                {
                    item.IsUserFavorite = favoriteVariantIds.Contains(item.VariantId);
                }
            }

            markFavorites(viewModel.FootwearCollection);
            markFavorites(viewModel.TopSellingProducts);
            markFavorites(viewModel.RacketSportsProducts);
            markFavorites(viewModel.ClimbingProducts);
            markFavorites(viewModel.TeamSportsProducts);
            ViewBag.ParentSports = parentSports;
            return View(viewModel);
        }




    }
}
