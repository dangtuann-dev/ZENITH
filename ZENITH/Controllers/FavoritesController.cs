using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;
using System.Security.Claims;
using ZENITH.AppData;
using ZENITH.Models;

namespace ZENITH.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public FavoritesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Request body sẽ được tự động map từ JSON sang int (request)
        public async Task<IActionResult> Toggle([FromBody] int variantId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                // Về lý thuyết, [Authorize] đã chặn điều này, nhưng vẫn kiểm tra an toàn
                return Json(new { Success = false, Message = "User not logged in." });
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == variantId);

            var isCurrentlyFavorite = existingFavorite != null;

            try
            {
                if (isCurrentlyFavorite)
                {
                    // HÀNH ĐỘNG 1: XÓA (UNLIKE)
                    _context.Favorites.Remove(existingFavorite!);
                    await _context.SaveChangesAsync();

                    // 💡 TRẢ VỀ ĐỐI TƯỢNG ẨN DANH (ANONYMOUS OBJECT)
                    return Json(new { Success = true, IsFavorite = false, Message = "Removed from favorites." });
                }
                else
                {
                    // HÀNH ĐỘNG 2: THÊM (LIKE)
                    var newFavorite = new Favorite
                    {
                        UserId = userId,
                        VariantId = variantId,
                        AddedAt = DateTime.Now
                    };
                    _context.Favorites.Add(newFavorite);
                    await _context.SaveChangesAsync();

                    // 💡 TRẢ VỀ ĐỐI TƯỢNG ẨN DANH (ANONYMOUS OBJECT)
                    return Json(new { Success = true, IsFavorite = true, Message = "Added to favorites." });
                }
            }
            catch (Exception ex)
            {
                // Trả về thất bại
                return Json(new { Success = false, Message = "Database error: " + ex.Message });
            }
        }


    }
}
