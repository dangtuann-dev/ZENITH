using ZENITH.AppData;
using ZENITH.Models;
using ZENITH.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZENITH.Services
{
    // Class triển khai Interface IMenuService
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection cho DbContext
        public MenuService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SportMenuItem>> GetMenuDataAsync()
        {
            // Lấy tất cả Sports, Categories, và liên kết N-M trong một truy vấn (performance)
            var allSports = await _context.Sports.AsNoTracking().ToListAsync();
            var allLinks = await _context.SportCategories.AsNoTracking().ToListAsync();
            var allCategories = await _context.Categories.AsNoTracking().ToListAsync();

            // 1. Lọc ra các Sport cấp cao nhất (ParentSportId = null)
            var topLevelSports = allSports
                .Where(s => s.ParentSportId == null)
                .OrderBy(s => s.DisplayOrder)
                .ToList();

            // 2. Bắt đầu xây dựng cấu trúc phân cấp đệ quy
            return BuildSportHierarchy(topLevelSports, allSports, allLinks, allCategories);
        }

        // Hàm đệ quy (Recursive) để xây dựng cấu trúc Sport Cha-Con
        private List<SportMenuItem> BuildSportHierarchy(
    IEnumerable<Sport> currentSports,
    List<Sport> allSports,
    List<SportCategory> allLinks,
    List<Category> allCategories)
        {
            var items = new List<SportMenuItem>();

            foreach (var sport in currentSports)
            {
                var menuItem = new SportMenuItem
                {
                    Id = sport.SportId,
                    Name = sport.SportName,
                    IconUrl = sport.IconUrl,
                    Categories = GetSportCategories(sport.SportId, allLinks, allCategories),
                };

                // Lấy các Sport Con (có ParentSportId là SportId hiện tại)
                var subSports = allSports
                    .Where(s => s.ParentSportId == sport.SportId)
                    .OrderBy(s => s.DisplayOrder);
                menuItem.SubSports = BuildSportHierarchy(subSports, allSports, allLinks, allCategories);

                items.Add(menuItem);
            }
            return items;
        }

        // Hàm ánh xạ N-M: Lấy Categories dựa trên SportId
        private List<CategoryViewModel> GetSportCategories(
            int sportId,
            List<SportCategory> allLinks,
            List<Category> allCategories)
        {
            // 1. Lấy CategoryIds từ bảng liên kết SportCategory
            var categoryIds = allLinks.Where(l => l.SportId == sportId).Select(l => l.CategoryId).ToList();

            // 2. Lọc ra và ánh xạ sang View Model
            return allCategories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryViewModel { Id = c.CategoryId, Name = c.CategoryName })
                .ToList();
        }
    }
}