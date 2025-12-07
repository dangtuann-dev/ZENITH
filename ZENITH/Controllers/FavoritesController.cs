﻿﻿﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ZENITH.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SessionCartKey = "cart.items";
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
 for (int i = 0; i < items.Count; i++)
            {
                items[i].ImageUrl = ResolveImageUrl(items[i].ImageUrl);
            }

            foreach (var item in items)
            {
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == item.ProductId && v.IsActive)
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
                

                if (!string.IsNullOrWhiteSpace(v.Attributes))
                {
                    return v.Attributes.Trim();
                }

                if (!string.IsNullOrWhiteSpace(v.VariantSku))
                {
                    return v.VariantSku;
                }

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

        public class MoveToCartRequest
        {
            public int? VariantId { get; set; }
            public int? Quantity { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Favorites/AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = _userManager.GetUserId(User);
            int variantId = request?.VariantId ?? 0;
            int quantity = request?.Quantity ?? 1;
            if (variantId <= 0 || quantity <= 0)
            {
                return BadRequest(new { success = false, message = "Thiếu hoặc không hợp lệ VariantId/Quantity." });
            }

            var variantActive = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId && v.IsActive);
            if (!variantActive)
            {
                return NotFound(new { success = false, message = "Biến thể không tồn tại hoặc không hoạt động." });
            }

            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }

                var sCart = HttpContext.Session.GetString(SessionCartKey);
                System.Collections.Generic.List<Models.CartItemSession> cartRaw;
                if (string.IsNullOrEmpty(sCart)) cartRaw = new System.Collections.Generic.List<Models.CartItemSession>();
                else { try { cartRaw = JsonSerializer.Deserialize<System.Collections.Generic.List<Models.CartItemSession>>(sCart) ?? new System.Collections.Generic.List<Models.CartItemSession>(); } catch { cartRaw = new System.Collections.Generic.List<Models.CartItemSession>(); } }
                var idx = cartRaw.FindIndex(x => x.VariantId == variantId);
                if (idx >= 0) cartRaw[idx].Quantity = Math.Max(1, cartRaw[idx].Quantity + quantity);
                else cartRaw.Add(new Models.CartItemSession { VariantId = variantId, Quantity = Math.Max(1, quantity) });
                
                var serialized = JsonSerializer.Serialize(cartRaw);
                HttpContext.Session.SetString(SessionCartKey, serialized);
                
                // Mark session as modified to ensure it's saved
                HttpContext.Session.SetString("_last_updated", DateTime.UtcNow.Ticks.ToString());
                
                // Force session to be saved - this is critical for distributed cache
                // The session will be committed when the response is sent, but we need to ensure
                // the distributed cache has the data before the next request
                try
                {
                    await HttpContext.Session.CommitAsync();
                }
                catch (Exception ex)
                {
                    // Log the error but continue - session will be saved when response is sent
                    System.Diagnostics.Debug.WriteLine($"Session commit error: {ex.Message}");
                }
                
                // Verify session was saved (read it back immediately from the same session)
                // This should work because we're reading from the same HttpContext
                var verify = HttpContext.Session.GetString(SessionCartKey);
                var verifyCount = 0;
                if (!string.IsNullOrEmpty(verify))
                {
                    try
                    {
                        var verifyList = JsonSerializer.Deserialize<System.Collections.Generic.List<Models.CartItemSession>>(verify);
                        verifyCount = verifyList?.Count ?? 0;
                    }
                    catch
                    {
                        verifyCount = 0;
                    }
                }
                
                // Return session ID for debugging
                return Ok(new { 
                    success = true, 
                    count = cartRaw.Count, 
                    verifiedCount = verifyCount,
                    sessionId = HttpContext.Session.Id,
                    serializedLength = serialized.Length
                });
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

        [HttpPost]
        [Route("Favorites/MoveToCart")]
        public async Task<IActionResult> MoveToCart([FromBody] MoveToCartRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để chuyển qua giỏ hàng." });
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

            // Remove from favorites if exists
            var existingFav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == variantId);
            if (existingFav != null)
            {
                _context.Favorites.Remove(existingFav);
            }

            // Add to cart or increase quantity
            var existingCart = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == variantId);
            if (existingCart != null)
            {
                existingCart.Quantity += quantity;
                existingCart.UpdatedAt = DateTime.UtcNow;
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

        [HttpGet]
        [AllowAnonymous]
        [Route("Favorites/CartPreview")]
        public async Task<IActionResult> CartPreview()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // Always try to load session first
                try
                {
                    await HttpContext.Session.LoadAsync();
                }
                catch { }
                
                var culture = new System.Globalization.CultureInfo("vi-VN");
                
                // Debug: Log session info
                var sessionId = HttpContext.Session.Id;
                var isAvailable = HttpContext.Session.IsAvailable;
                
                // Read session data
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                System.Collections.Generic.List<Models.CartItemSession> cartRaw;
                if (string.IsNullOrEmpty(sCart)) 
                {
                    cartRaw = new System.Collections.Generic.List<Models.CartItemSession>();
                }
                else 
                { 
                    try 
                    { 
                        cartRaw = JsonSerializer.Deserialize<System.Collections.Generic.List<Models.CartItemSession>>(sCart) ?? new System.Collections.Generic.List<Models.CartItemSession>(); 
                    } 
                    catch 
                    { 
                        cartRaw = new System.Collections.Generic.List<Models.CartItemSession>(); 
                    } 
                }

                // Group by VariantId and sum quantities
                cartRaw = cartRaw
                    .Where(x => x != null && x.VariantId > 0)
                    .GroupBy(x => x.VariantId)
                    .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                    .ToList();

                // Filter out invalid variants
                var validCartItems = cartRaw
                    .Where(ci => ci.VariantId > 0 && ci.Quantity > 0)
                    .ToList();

                // Debug info - always include it
                var debugInfo = new
                {
                    sessionId = sessionId,
                    isAvailable = isAvailable,
                    cartRawCount = cartRaw.Count,
                    validCartItemsCount = validCartItems.Count,
                    sCartLength = sCart?.Length ?? 0,
                    sCartIsEmpty = string.IsNullOrEmpty(sCart),
                    sCartPreview = sCart != null ? sCart.Substring(0, Math.Min(100, sCart.Length)) : "null"
                };

                if (!validCartItems.Any())
                {
                    return Ok(new { 
                        count = 0, 
                        subtotalFormatted = "0 VND", 
                        items = Array.Empty<object>(),
                        debug = debugInfo
                    });
                }

                var ids = validCartItems.Select(x => x.VariantId).Distinct().ToList();
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => ids.Contains(v.VariantId) && v.IsActive)
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .ToListAsync();

                // Filter cart items to only include valid variants with products
                var validItemsWithVariants = validCartItems
                    .Where(ci => variants.Any(v => v.VariantId == ci.VariantId && v.Product != null))
                    .ToList();

                int countG = validItemsWithVariants.Sum(c => Math.Max(c.Quantity, 1));
                decimal subtotalG = validItemsWithVariants.Sum(c => 
                {
                    var v = variants.FirstOrDefault(v => v.VariantId == c.VariantId);
                    return (v?.SalePrice ?? v?.Price ?? 0) * Math.Max(c.Quantity, 1);
                });

                var recentG = validItemsWithVariants.Take(3).Select(ci => new {
                    v = variants.FirstOrDefault(x => x.VariantId == ci.VariantId && x.Product != null),
                    qty = Math.Max(ci.Quantity, 1)
                }).Where(x => x.v != null && x.v.Product != null).Select(x => new {
                    productId = x.v!.ProductId,
                    productName = x.v!.Product!.ProductName,
                    imageUrlRaw = x.v!.Product!.ProductImages
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    quantity = x.qty,
                    priceEach = x.v!.SalePrice ?? x.v!.Price
                }).ToList();

                string ResolveImageUrlG(string? s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return Url.Content("~/image/default.avif");
                    s = s.Trim();
                    var lower = s.ToLowerInvariant();
                    if (lower.StartsWith("http://") || lower.StartsWith("https://")) return s;
                    if (s.StartsWith("~/")) return Url.Content(s);
                    if (s.StartsWith("/")) return Url.Content(s);
                    var marker = "wwwroot";
                    int idx = lower.IndexOf(marker);
                    if (idx >= 0)
                    {
                        var tail = s.Substring(idx + marker.Length).Replace('\\', '/');
                        return Url.Content("~" + (tail.StartsWith("/") ? tail : "/" + tail));
                    }
                    s = s.Replace('\\', '/');
                    s = s.TrimStart('~');
                    return Url.Content("~/" + s.TrimStart('/'));
                }

                var itemsG = recentG.Select(x => new
                {
                    productId = x.productId,
                    productName = x.productName,
                    imgUrl = ResolveImageUrlG(x.imageUrlRaw),
                    quantity = x.quantity,
                    priceEachFormatted = x.priceEach.ToString("N0", culture) + " VND"
                }).ToList();

                // Update debug info with additional fields
                var debugInfoFinal = new
                {
                    sessionId = sessionId,
                    isAvailable = isAvailable,
                    cartRawCount = cartRaw.Count,
                    validCartItemsCount = validCartItems.Count,
                    validItemsWithVariantsCount = validItemsWithVariants.Count,
                    variantsCount = variants.Count,
                    sCartLength = sCart?.Length ?? 0,
                    sCartIsEmpty = string.IsNullOrEmpty(sCart),
                    sCartPreview = sCart != null ? sCart.Substring(0, Math.Min(100, sCart.Length)) : "null"
                };

                return Ok(new { 
                    count = countG, 
                    subtotalFormatted = subtotalG.ToString("N0", culture) + " VND", 
                    items = itemsG,
                    debug = debugInfoFinal
                });
            }

            var culture2 = new System.Globalization.CultureInfo("vi-VN");
            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .ToListAsync();
            var merged = cartItems
                .GroupBy(c => c.VariantId)
                .Select(g => new { Variant = g.First().ProductVariant, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            int count = merged.Sum(m => m.Quantity);
            decimal subtotal = merged.Sum(m => (m.Variant.SalePrice ?? m.Variant.Price) * m.Quantity);

            var recent = merged.Take(3).Select(m => new
            {
                productId = m.Variant.ProductId,
                productName = m.Variant.Product.ProductName,
                imageUrlRaw = m.Variant.Product.ProductImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),
                quantity = m.Quantity,
                priceEach = m.Variant.SalePrice ?? m.Variant.Price
            }).ToList();

            string ResolveImageUrl(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return Url.Content("~/image/default.avif");
                s = s.Trim();
                var lower = s.ToLowerInvariant();
                if (lower.StartsWith("http://") || lower.StartsWith("https://")) return s;
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return Url.Content(s);
                var marker = "wwwroot";
                int idx = lower.IndexOf(marker);
                if (idx >= 0)
                {
                    var tail = s.Substring(idx + marker.Length).Replace('\\', '/');
                    return Url.Content("~" + (tail.StartsWith("/") ? tail : "/" + tail));
                }
                s = s.Replace('\\', '/');
                s = s.TrimStart('~');
                return Url.Content("~/" + s.TrimStart('/'));
            }

            var items = recent.Select(x => new
            {
                productId = x.productId,
                productName = x.productName,
                imgUrl = ResolveImageUrl(x.imageUrlRaw),
                quantity = x.quantity,
                priceEachFormatted = x.priceEach.ToString("N0", culture2) + " VND"
            }).ToList();

            return Ok(new { count, subtotalFormatted = subtotal.ToString("N0", culture2) + " VND", items });
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
