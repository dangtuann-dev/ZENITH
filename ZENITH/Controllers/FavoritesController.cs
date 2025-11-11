using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

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
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return View(new System.Collections.Generic.List<ZENITH.ViewModels.FavoritesIndexItemViewModel>());
            }

            string ResolveImageUrl(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
                var s = path.Trim();

                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return s;
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return s;

                var lower = s.ToLowerInvariant();
                int idxWwwroot = lower.IndexOf("wwwroot");
                if (idxWwwroot >= 0)
                {
                    var after = s.Substring(idxWwwroot + "wwwroot".Length).Replace('\\', '/');
                    return Url.Content("~" + (after.StartsWith("/") ? after : "/" + after));
                }

                foreach (var marker in new[] {
                    "/uploads/", "uploads/", "\\uploads\\",
                    "/images/", "images/", "\\images\\",
                    "/image/", "image/", "\\image\\"
                })
                {
                    int idx = lower.IndexOf(marker);
                    if (idx >= 0)
                    {
                        var tail = s.Substring(idx).Replace('\\', '/');
                        return Url.Content("~" + (tail.StartsWith("/") ? tail : "/" + tail));
                    }
                }

                s = s.Replace('\\', '/');
                return Url.Content("~/" + s.TrimStart('/'));
            }

            // Project raw data first so EF can translate to SQL, then normalize image URLs in memory
            var items = await _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt)
                .Include(f => f.ProductVariant)
                    .ThenInclude(v => v.Product)
                .ThenInclude(p => p.ProductImages)
                .Select(f => new ZENITH.ViewModels.FavoritesIndexItemViewModel
                {
                    VariantId = f.VariantId,
                    ProductId = f.ProductVariant.ProductId,
                    ProductName = f.ProductVariant.Product.ProductName,
                    ImageUrl = f.ProductVariant.Product.ProductImages
                            .OrderByDescending(i => i.IsPrimary)
                            .ThenBy(i => i.DisplayOrder)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault(),
                    Price = f.ProductVariant.SalePrice ?? f.ProductVariant.Price,
                    SalePrice = f.ProductVariant.SalePrice,
                    StockQuantity = f.ProductVariant.StockQuantity
                })
                .ToListAsync();

            // Normalize image URL paths after fetching data from the database
            for (int i = 0; i < items.Count; i++)
            {
                items[i].ImageUrl = ResolveImageUrl(items[i].ImageUrl);
            }

            // Populate variant lists for each product
            foreach (var item in items)
            {
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == item.ProductId && v.IsActive)
                    .Include(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                    .OrderBy(v => v.SalePrice ?? v.Price)
                    .ToListAsync();

                var options = variants.Select(v => new ZENITH.ViewModels.VariantOptionViewModel
                {
                    VariantId = v.VariantId,
                    Text = BuildVariantText(v),
                    Price = v.Price,
                    SalePrice = v.SalePrice,
                    StockQuantity = v.StockQuantity,
                    IsSelected = v.VariantId == item.VariantId
                }).ToList();

                item.Variants = options;
            }

            string BuildVariantText(ProductVariant v)
            {
                // Ưu tiên hiển thị theo bảng liên kết VariantAttributeValues (ví dụ: "Size: L, Color: Red")
                if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                {
                    var parts = v.VariantAttributeValues
                        .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                        .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}");
                    return string.Join(", ", parts);
                }

                // Fallback 1: dùng chuỗi mô tả thuộc tính trong cột ProductVariant.Attributes nếu có
                if (!string.IsNullOrWhiteSpace(v.Attributes))
                {
                    return v.Attributes.Trim();
                }

                // Fallback 2: dùng SKU của biến thể nếu không có dữ liệu thuộc tính
                if (!string.IsNullOrWhiteSpace(v.VariantSku))
                {
                    return v.VariantSku;
                }

                // Fallback cuối: hiển thị theo mã VariantId
                return $"SKU {v.VariantId}";
            }

            return View(items);
        }
        public class ChangeFavoriteVariantRequest
        {
            public int? OldVariantId { get; set; }
            public int? NewVariantId { get; set; }
        }

        [HttpPost]
        [Route("Favorites/ChangeVariant")]
        public async Task<IActionResult> ChangeVariant([FromBody] ChangeFavoriteVariantRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Bạn cần đăng nhập." });

            int oldId = request?.OldVariantId ?? 0;
            int newId = request?.NewVariantId ?? 0;
            if (oldId == 0 || newId == 0 || oldId == newId)
            {
                return BadRequest(new { success = false, message = "Thiếu hoặc không hợp lệ OldVariantId/NewVariantId." });
            }

            var oldFav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == oldId);
            if (oldFav == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy mục yêu thích cần đổi." });
            }

            var newVariant = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Product.ProductImages)
                .FirstOrDefaultAsync(v => v.VariantId == newId);
            if (newVariant == null)
            {
                return NotFound(new { success = false, message = "Biến thể mới không tồn tại." });
            }

            // Xóa favorite cũ
            _context.Favorites.Remove(oldFav);

            // Nếu biến thể mới chưa được yêu thích thì thêm vào
            var existsNew = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.VariantId == newId);
            if (!existsNew)
            {
                _context.Favorites.Add(new Favorite
                {
                    UserId = userId,
                    VariantId = newId,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Trả dữ liệu mới để UI cập nhật
            var price = newVariant.SalePrice ?? newVariant.Price;
            var img = newVariant.Product.ProductImages
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();

            string Normalize(string? p)
            {
                if (string.IsNullOrWhiteSpace(p)) return Url.Content("~/image/default.avif");
                var s = p.Replace('\\', '/');
                if (s.StartsWith("http") || s.StartsWith("/")) return s;
                int idx = s.ToLowerInvariant().IndexOf("wwwroot");
                if (idx >= 0)
                {
                    var tail = s.Substring(idx + "wwwroot".Length);
                    return Url.Content("~" + (tail.StartsWith("/") ? tail : "/" + tail));
                }
                return Url.Content("~/" + s.TrimStart('/'));
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");
            return Ok(new
            {
                success = true,
                newVariantId = newId,
                priceFormatted = string.Format(culture, "{0:N0}", price) + " VND",
                stockQuantity = newVariant.StockQuantity,
                imgUrl = Normalize(img),
                productId = newVariant.ProductId,
                productName = newVariant.Product.ProductName
            });
        }
        // DTO to bind incoming requests from JSON or form/query
        public class ToggleFavoriteRequest
        {
            public int? VariantId { get; set; }
            public int? ProductId { get; set; } // fallback if frontend sends productId but it’s actually variantId
        }

        [HttpPost]
        [Route("Favorites/ToggleFavorite")]
        public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để sử dụng chức năng yêu thích." });
            }

            // Resolve variantId from request body or form/query fallback
            int variantId = request?.VariantId ?? request?.ProductId ?? 0;
            if (variantId == 0)
            {
                // Try to get from form/query if body was not provided
                if (int.TryParse(Request.Form["variantId"], out var vid)) variantId = vid;
                else if (int.TryParse(Request.Form["productId"], out var pid)) variantId = pid;
                else if (int.TryParse(Request.Query["variantId"], out var qvid)) variantId = qvid;
                else if (int.TryParse(Request.Query["productId"], out var qpid)) variantId = qpid;
            }

            if (variantId == 0)
            {
                return BadRequest(new { success = false, message = "Thiếu VariantId hoặc ProductId." });
            }

            // Ensure variant exists
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId);
            if (!variantExists)
            {
                return NotFound(new { success = false, message = "Biến thể sản phẩm không tồn tại." });
            }

            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == variantId);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, isFavorited = false, message = "Đã bỏ yêu thích sản phẩm." });
            }
            else
            {
                var favorite = new Favorite
                {
                    UserId = userId,
                    VariantId = variantId,
                    AddedAt = DateTime.UtcNow
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, isFavorited = true, message = "Đã thêm sản phẩm vào yêu thích." });
            }
        }

        public class AddToCartRequest
        {
            public int? VariantId { get; set; }
            public int? Quantity { get; set; }
        }

        [HttpPost]
        [Route("Favorites/AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để thêm vào giỏ hàng." });
            }

            int variantId = request?.VariantId ?? 0;
            int quantity = request?.Quantity ?? 1;
            if (variantId <= 0 || quantity <= 0)
            {
                return BadRequest(new { success = false, message = "Thiếu hoặc không hợp lệ VariantId/Quantity." });
            }

            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == variantId && v.IsActive);
            if (variant == null)
            {
                return NotFound(new { success = false, message = "Biến thể không tồn tại hoặc không hoạt động." });
            }

            var existing = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == variantId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    VariantId = variantId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet]
        [Route("Favorites/Recent")]
        public async Task<IActionResult> Recent()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // Không bắt buộc redirect khi chưa đăng nhập; trả về danh sách trống
                return Ok(new { items = Array.Empty<object>() });
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");

            var rawItems = await _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt)
                .Include(f => f.ProductVariant)
                    .ThenInclude(v => v.Product)
                .ThenInclude(p => p.ProductImages)
                .Take(3)
                .Select(f => new
                {
                    productId = f.ProductVariant.ProductId,
                    productName = f.ProductVariant.Product.ProductName,
                    imageUrlRaw = f.ProductVariant.Product.ProductImages
                        .OrderByDescending(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    price = f.ProductVariant.SalePrice ?? f.ProductVariant.Price
                })
                .ToListAsync();

            string ResolveImageUrl(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
                var s = path.Trim();

                // URL tuyệt đối
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return s;

                // Đường dẫn đã chuẩn webroot
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return s;

                // Nếu là đường dẫn tuyệt đối trên hệ thống file (Windows/Mac/Linux), cố gắng chuyển về web path
                // Ví dụ: D:\Web Profile\ZENITH\ZENITH\wwwroot\uploads\abc.jpg -> /uploads/abc.jpg
                //       /var/app/wwwroot/images/p.jpg -> /images/p.jpg
                var lower = s.ToLowerInvariant();
                int idxWwwroot = lower.IndexOf("wwwroot");
                if (idxWwwroot >= 0)
                {
                    var after = s.Substring(idxWwwroot + "wwwroot".Length).Replace('\\', '/');
                    return Url.Content("~" + (after.StartsWith("/") ? after : "/" + after));
                }

                // Nếu chứa thư mục phổ biến như uploads hoặc images ở bất kỳ vị trí nào, cắt từ đó trở đi
                foreach (var marker in new[] { 
                    "/uploads/", "uploads/", "\\uploads\\",
                    "/images/", "images/", "\\images\\",
                    "/image/", "image/", "\\image\\"
                })
                {
                    int idx = lower.IndexOf(marker);
                    if (idx >= 0)
                    {
                        var tail = s.Substring(idx).Replace('\\', '/');
                        return Url.Content("~" + (tail.StartsWith("/") ? tail : "/" + tail));
                    }
                }

                // Chuẩn hoá backslashes -> forward slashes rồi đưa về ~/relative
                s = s.Replace('\\', '/');
                return Url.Content("~/" + s.TrimStart('/'));
            }

            var items = rawItems.Select(x => new
            {
                productId = x.productId,
                productName = x.productName,
                imgUrl = ResolveImageUrl(x.imageUrlRaw),
                priceFormatted = x.price.ToString("N0", culture)
            }).ToList();

            return Ok(new { items });
        }

        [HttpPost]
        [Route("Favorites/Add")]
        public async Task<IActionResult> Add([FromBody] ToggleFavoriteRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            int variantId = request?.VariantId ?? request?.ProductId ?? 0;
            if (variantId == 0) return BadRequest();

            var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.VariantId == variantId);
            if (exists) return Ok(new { success = true, isFavorited = true });

            _context.Favorites.Add(new Favorite { UserId = userId, VariantId = variantId, AddedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { success = true, isFavorited = true });
        }

        [HttpPost]
        [Route("Favorites/Remove")]
        public async Task<IActionResult> Remove([FromBody] ToggleFavoriteRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            int variantId = request?.VariantId ?? request?.ProductId ?? 0;
            if (variantId == 0) return BadRequest();

            var existing = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == variantId);
            if (existing == null) return Ok(new { success = true, isFavorited = false });

            _context.Favorites.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, isFavorited = false });
        }
    }
}
