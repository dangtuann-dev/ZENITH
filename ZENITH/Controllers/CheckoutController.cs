using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.ViewModels;
using Microsoft.Data.SqlClient;

namespace ZENITH.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .ToListAsync();

            string ResolveImageUrl(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
                var s = path.Trim();
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return s;
                var lower = s.ToLowerInvariant();
                int idxWwwroot = lower.IndexOf("wwwroot");
                if (idxWwwroot >= 0)
                {
                    var after = s.Substring(idxWwwroot + "wwwroot".Length).Replace('\\', '/');
                    return Url.Content("~" + (after.StartsWith("/") ? after : "/" + after));
                }
                foreach (var marker in new[] { "/uploads/", "uploads/", "\\uploads\\", "/images/", "images/", "\\images\\", "/image/", "image/", "\\image\\" })
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

            string BuildVariantText(Models.ProductVariant v)
            {
                if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                {
                    var parts = v.VariantAttributeValues
                        .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                        .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}");
                    return string.Join(", ", parts);
                }
                if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                return $"SKU {v.VariantId}";
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");
            int itemCount = cartItems.Sum(c => c.Quantity);
            decimal subtotal = cartItems.Sum(c => (c.ProductVariant.SalePrice ?? c.ProductVariant.Price) * c.Quantity);
            decimal shipping = cartItems.Count * 15000m;
            decimal total = subtotal + shipping;

            var items = cartItems.Select(ci => new CheckoutItemViewModel
            {
                VariantId = ci.VariantId,
                ProductId = ci.ProductVariant.ProductId,
                ProductName = ci.ProductVariant.Product.ProductName,
                ImageUrl = ResolveImageUrl(ci.ProductVariant.Product.ProductImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()),
                Quantity = ci.Quantity,
                UnitPrice = ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price,
                LineTotal = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity,
                AttributesText = BuildVariantText(ci.ProductVariant),
                StockQuantity = ci.ProductVariant.StockQuantity,
                UnitPriceFormatted = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price).ToString("N0", culture) + " VND",
                LineTotalFormatted = (((ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity)).ToString("N0", culture) + " VND"
            }).ToList();

            foreach (var it in items)
            {
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == it.ProductId && v.IsActive)
                    .Include(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                    .OrderBy(v => v.SalePrice ?? v.Price)
                    .ToListAsync();
                it.Variants = variants.Select(v => new ZENITH.ViewModels.VariantOptionViewModel
                {
                    VariantId = v.VariantId,
                    Text = BuildVariantText(v),
                    Price = v.Price,
                    SalePrice = v.SalePrice,
                    StockQuantity = v.StockQuantity,
                    IsSelected = v.VariantId == it.VariantId
                }).ToList();
            }

            var model = new CheckoutIndexViewModel
            {
                Items = items,
                ItemCount = itemCount,
                Subtotal = subtotal,
                Shipping = shipping,
                Total = total,
                SubtotalFormatted = subtotal.ToString("N0", culture) + " VND",
                ShippingFormatted = shipping.ToString("N0", culture) + " VND",
                TotalFormatted = total.ToString("N0", culture) + " VND"
            };

            return View(model);
        }

        public class UpdateQuantityRequest
        {
            public int? VariantId { get; set; }
            public int? Delta { get; set; }
        }

        [HttpPost]
        [Route("Checkout/UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });
            int variantId = request?.VariantId ?? 0;
            int delta = request?.Delta ?? 0;
            if (variantId <= 0 || delta == 0) return BadRequest(new { success = false });
            var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == variantId);
            if (item == null) return NotFound(new { success = false });
            item.Quantity += delta;
            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class RemoveItemRequest
        {
            public int? VariantId { get; set; }
        }

        [HttpPost]
        [Route("Checkout/RemoveItem")]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });
            int variantId = request?.VariantId ?? 0;
            if (variantId <= 0) return BadRequest(new { success = false });
            var item = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == variantId);
            if (item == null) return NotFound(new { success = false });
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class SaveItemRequest
        {
            public int? VariantId { get; set; }
        }

        [HttpPost]
        [Route("Checkout/SaveItem")]
        public async Task<IActionResult> SaveItem([FromBody] SaveItemRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });
            int variantId = request?.VariantId ?? 0;
            if (variantId <= 0) return BadRequest(new { success = false });

            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == variantId && v.IsActive);
            if (variant == null) return NotFound(new { success = false });

            var existedFav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.VariantId == variantId);
            if (existedFav == null)
            {
                _context.Favorites.Add(new ZENITH.Models.Favorite
                {
                    UserId = userId,
                    VariantId = variantId,
                    AddedAt = DateTime.UtcNow
                });
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == variantId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class SaveAddressRequest
        {
            public int? AddressId { get; set; }
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? AddressLine { get; set; }
            public string? Ward { get; set; }
            public string? District { get; set; }
            public string? City { get; set; }
        }

        [HttpPost]
        [Route("Checkout/SaveAddress")]
        public async Task<IActionResult> SaveAddress([FromBody] SaveAddressRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });

            string fullName = (request?.FullName ?? string.Empty).Trim();
            string phone = (request?.Phone ?? string.Empty).Trim();
            string addressLine = (request?.AddressLine ?? string.Empty).Trim();
            string ward = (request?.Ward ?? string.Empty).Trim();
            string district = (request?.District ?? string.Empty).Trim();
            string city = (request?.City ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(addressLine)
                || string.IsNullOrEmpty(ward) || string.IsNullOrEmpty(district) || string.IsNullOrEmpty(city))
            {
                return BadRequest(new { success = false });
            }

            int addrId = request?.AddressId ?? 0;
            ZENITH.Models.Address? entity = null;
            if (addrId > 0)
            {
                entity = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addrId && a.UserId == userId);
                if (entity == null) return NotFound(new { success = false });
                entity.FullName = fullName;
                entity.Phone = phone;
                entity.AddressLine = addressLine;
                entity.Ward = ward;
                entity.District = district;
                entity.City = city;
            }
            else
            {
                bool firstAddress = !await _context.Addresses.AnyAsync(a => a.UserId == userId);
                entity = new ZENITH.Models.Address
                {
                    UserId = userId,
                    FullName = fullName,
                    Phone = phone,
                    AddressLine = addressLine,
                    Ward = ward,
                    District = district,
                    City = city,
                    IsDefault = firstAddress
                };
                _context.Addresses.Add(entity);
            }

            await _context.SaveChangesAsync();

            var display = string.Join(", ", new[] { entity.AddressLine, entity.Ward, entity.District, entity.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return Ok(new
            {
                success = true,
                address = new
                {
                    addressId = entity.AddressId,
                    fullName = entity.FullName,
                    phone = entity.Phone,
                    addressLine = entity.AddressLine,
                    ward = entity.Ward,
                    district = entity.District,
                    city = entity.City,
                    isDefault = entity.IsDefault,
                    displayText = display
                }
            });
        }

        public class DeleteAddressRequest
        {
            public int? AddressId { get; set; }
        }

        [HttpPost]
        [Route("Checkout/DeleteAddress")]
        public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });
            int id = request?.AddressId ?? 0;
            if (id <= 0) return BadRequest(new { success = false });

            var entity = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            if (entity == null) return NotFound(new { success = false });

            _context.Addresses.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        public class ChangeVariantRequest
        {
            public int? OldVariantId { get; set; }
            public int? NewVariantId { get; set; }
        }

        [HttpPost]
        [Route("Checkout/ChangeVariant")]
        public async Task<IActionResult> ChangeVariant([FromBody] ChangeVariantRequest request)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false });
            int oldId = request?.OldVariantId ?? 0;
            int newId = request?.NewVariantId ?? 0;
            if (oldId <= 0 || newId <= 0 || oldId == newId) return BadRequest(new { success = false });
            var oldItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == oldId);
            if (oldItem == null) return NotFound(new { success = false });
            var newVariant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == newId && v.IsActive);
            if (newVariant == null) return NotFound(new { success = false });
            var existingNew = await _context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.VariantId == newId);
            if (existingNew != null)
            {
                existingNew.Quantity += oldItem.Quantity;
                existingNew.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Remove(oldItem);
            }
            else
            {
                oldItem.VariantId = newId;
                oldItem.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpGet]
        [Route("Checkout/GetCartSummary")]
        public async Task<IActionResult> GetCartSummary()
        {
            var userId = _userManager.GetUserId(User);
            var culture = new System.Globalization.CultureInfo("vi-VN");
            if (string.IsNullOrEmpty(userId))
            {
                return Ok(new
                {
                    count = 0,
                    subtotal = 0m,
                    shipping = 0m,
                    tax = 0m,
                    total = 0m,
                    subtotalFormatted = 0m.ToString("N0", culture) + " VND",
                    shippingFormatted = 0m.ToString("N0", culture) + " VND",
                    totalFormatted = 0m.ToString("N0", culture) + " VND"
                });
            }

            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Include(c => c.ProductVariant)
                .ToListAsync();

            int count = cartItems.Sum(c => c.Quantity);
            decimal subtotal = cartItems.Sum(c => (c.ProductVariant.SalePrice ?? c.ProductVariant.Price) * c.Quantity);
            decimal shipping = cartItems.Count * 15000m;
            decimal tax = 0m;
            decimal total = subtotal + shipping + tax;

            return Ok(new
            {
                count,
                subtotal,
                shipping,
                tax,
                total,
                subtotalFormatted = subtotal.ToString("N0", culture) + " VND",
                shippingFormatted = shipping.ToString("N0", culture) + " VND",
                totalFormatted = total.ToString("N0", culture) + " VND"
            });
        }
        public IActionResult Shipping()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var cartItems = _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .ToList();

            string ResolveImageUrl(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
                var s = path.Trim();
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return s;
                var lower = s.ToLowerInvariant();
                int idxWwwroot = lower.IndexOf("wwwroot");
                if (idxWwwroot >= 0)
                {
                    var after = s.Substring(idxWwwroot + "wwwroot".Length).Replace('\\', '/');
                    return Url.Content("~" + (after.StartsWith("/") ? after : "/" + after));
                }
                foreach (var marker in new[] { "/uploads/", "uploads/", "\\uploads\\", "/images/", "images/", "\\images\\", "/image/", "image/", "\\image\\" })
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

            string BuildVariantText(Models.ProductVariant v)
            {
                if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                {
                    var parts = v.VariantAttributeValues
                        .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                        .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}");
                    return string.Join(", ", parts);
                }
                if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                return $"SKU {v.VariantId}";
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");
            int itemCount = cartItems.Sum(c => c.Quantity);
            decimal subtotal = cartItems.Sum(c => (c.ProductVariant.SalePrice ?? c.ProductVariant.Price) * c.Quantity);
            decimal shipping = cartItems.Count * 15000m;
            decimal total = subtotal + shipping;

            var items = cartItems.Select(ci => new CheckoutItemViewModel
            {
                VariantId = ci.VariantId,
                ProductId = ci.ProductVariant.ProductId,
                ProductName = ci.ProductVariant.Product.ProductName,
                ImageUrl = ResolveImageUrl(ci.ProductVariant.Product.ProductImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()),
                Quantity = ci.Quantity,
                UnitPrice = ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price,
                LineTotal = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity,
                AttributesText = BuildVariantText(ci.ProductVariant),
                StockQuantity = ci.ProductVariant.StockQuantity,
                UnitPriceFormatted = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price).ToString("N0", culture) + " VND",
                LineTotalFormatted = (((ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity)).ToString("N0", culture) + " VND"
            }).ToList();

            var addresses = _context.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.AddressId)
                .Select(a => new AddressItemViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    AddressLine = a.AddressLine,
                    Ward = a.Ward,
                    District = a.District,
                    City = a.City,
                    IsDefault = a.IsDefault
                })
                .ToList();

            int? selectedId = addresses.FirstOrDefault(a => a.IsDefault)?.AddressId;
            selectedId ??= addresses.FirstOrDefault()?.AddressId;

            var model = new ShippingViewModel
            {
                Addresses = addresses,
                SelectedAddressId = selectedId,
                Items = items,
                ItemCount = itemCount,
                Subtotal = subtotal,
                Shipping = shipping,
                Total = total,
                SubtotalFormatted = subtotal.ToString("N0", culture) + " VND",
                ShippingFormatted = shipping.ToString("N0", culture) + " VND",
                TotalFormatted = total.ToString("N0", culture) + " VND"
            };

            return View(model);
        }
        public IActionResult Payment(int? addressId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var cartItems = _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .ToList();

            string ResolveImageUrl(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
                var s = path.Trim();
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
                if (s.StartsWith("~/")) return Url.Content(s);
                if (s.StartsWith("/")) return s;
                var lower = s.ToLowerInvariant();
                int idxWwwroot = lower.IndexOf("wwwroot");
                if (idxWwwroot >= 0)
                {
                    var after = s.Substring(idxWwwroot + "wwwroot".Length).Replace('\\', '/');
                    return Url.Content("~" + (after.StartsWith("/") ? after : "/" + after));
                }
                foreach (var marker in new[] { "/uploads/", "uploads/", "\\uploads\\", "/images/", "images/", "\\images\\", "/image/", "image/", "\\image\\" })
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

            string BuildVariantText(Models.ProductVariant v)
            {
                if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                {
                    var parts = v.VariantAttributeValues
                        .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                        .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}");
                    return string.Join(", ", parts);
                }
                if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                return $"SKU {v.VariantId}";
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");
            int itemCount = cartItems.Sum(c => c.Quantity);
            decimal subtotal = cartItems.Sum(c => (c.ProductVariant.SalePrice ?? c.ProductVariant.Price) * c.Quantity);
            decimal shipping = cartItems.Count * 15000m;
            decimal total = subtotal + shipping;

            var items = cartItems.Select(ci => new CheckoutItemViewModel
            {
                VariantId = ci.VariantId,
                ProductId = ci.ProductVariant.ProductId,
                ProductName = ci.ProductVariant.Product.ProductName,
                ImageUrl = ResolveImageUrl(ci.ProductVariant.Product.ProductImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()),
                Quantity = ci.Quantity,
                UnitPrice = ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price,
                LineTotal = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity,
                AttributesText = BuildVariantText(ci.ProductVariant),
                StockQuantity = ci.ProductVariant.StockQuantity,
                UnitPriceFormatted = (ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price).ToString("N0", culture) + " VND",
                LineTotalFormatted = (((ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price) * ci.Quantity)).ToString("N0", culture) + " VND"
            }).ToList();

            var addresses = _context.Addresses
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.AddressId)
                .Select(a => new AddressItemViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    AddressLine = a.AddressLine,
                    Ward = a.Ward,
                    District = a.District,
                    City = a.City,
                    IsDefault = a.IsDefault
                })
                .ToList();

            int? selectedId = addressId;
            selectedId ??= addresses.FirstOrDefault(a => a.IsDefault)?.AddressId;
            selectedId ??= addresses.FirstOrDefault()?.AddressId;

            var selected = addresses.FirstOrDefault(a => a.AddressId == (selectedId ?? 0));

            var model = new PaymentViewModel
            {
                SelectedAddress = selected,
                Items = items,
                ItemCount = itemCount,
                Subtotal = subtotal,
                Shipping = shipping,
                Total = total,
                SubtotalFormatted = subtotal.ToString("N0", culture) + " VND",
                ShippingFormatted = shipping.ToString("N0", culture) + " VND",
                TotalFormatted = total.ToString("N0", culture) + " VND"
            };

            return View(model);
        }

        public class PlaceOrderRequest
        {
            public int? AddressId { get; set; }
            public string? ShippingMethod { get; set; } // standard | express
            public decimal? ShippingRate { get; set; } // per item line
            public string? PaymentType { get; set; } // card | cod
            public string? CardHolder { get; set; }
            public string? CardNumber { get; set; }
            public string? ExpiryDate { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Checkout/PlaceOrder")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Not logged in" });
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized(new { success = false, message = "Not logged in" });
                if (string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    return BadRequest(new { success = false, message = "Vui lòng hoàn tất thông tin cá nhân của bạn trong trang Hồ sơ trước khi thanh toán." });
                }

                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.ProductVariant)
                        .ThenInclude(v => v.VariantAttributeValues)
                            .ThenInclude(vav => vav.AttributeValue)
                                .ThenInclude(av => av.Attribute)
                    .ToListAsync();
                if (cartItems.Count == 0) return BadRequest(new { success = false, message = "Cart is empty" });

                int? addressId = request?.AddressId;
                var address = addressId.HasValue
                    ? await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId)
                    : await _context.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.AddressId).FirstOrDefaultAsync(a => a.UserId == userId);
                if (address == null) return BadRequest(new { success = false, message = "Address not found" });

                decimal subtotal = cartItems.Sum(c => (c.ProductVariant.SalePrice ?? c.ProductVariant.Price) * c.Quantity);
                int lineCount = cartItems.Count;
                string method = (request?.ShippingMethod ?? "standard").ToLowerInvariant();
                decimal perLine = method == "express" ? 30000m : 15000m;
                if (request?.ShippingRate is decimal sr && sr > 0) perLine = sr; // allow client-provided consistent rate
                decimal shippingFee = perLine * lineCount;
                decimal total = subtotal + shippingFee;

                string ptype = "cod";

                string orderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0,6)}";
                var order = new ZENITH.Models.Order
                {
                    UserId = userId,
                    AddressId = address.AddressId,
                    PaymentType = ptype.ToUpperInvariant(),
                    OrderCode = orderCode,
                    Subtotal = subtotal,
                    ShippingFee = shippingFee,
                    Discount = 0,
                    TotalAmount = total,
                    PaymentStatus = "Pending",
                    OrderStatus = "Processing",
                    Note = null,
                    OrderDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Orders.Add(order);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    if (!string.IsNullOrWhiteSpace(msg) && (msg.Contains("Invalid column name 'PaymentType'") || msg.Contains("Cannot insert the value NULL into column 'PaymentId'")))
                    {
                        await EnsurePaymentSchemaAsync();
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        throw;
                    }
                }

                string BuildVariantText(ZENITH.Models.ProductVariant v)
                {
                    if (v.VariantAttributeValues != null && v.VariantAttributeValues.Any())
                    {
                        var parts = v.VariantAttributeValues
                            .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                            .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}");
                        return string.Join(", ", parts);
                    }
                    if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                    if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                    return $"SKU {v.VariantId}";
                }

                foreach (var ci in cartItems)
                {
                    var unit = ci.ProductVariant.SalePrice ?? ci.ProductVariant.Price;
                    var item = new ZENITH.Models.OrderItem
                    {
                        OrderId = order.OrderId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        UnitPrice = unit,
                        TotalPrice = unit * ci.Quantity,
                        VariantDescription = BuildVariantText(ci.ProductVariant)
                    };
                    _context.OrderItems.Add(item);

                    ci.ProductVariant.StockQuantity = Math.Max(0, ci.ProductVariant.StockQuantity - ci.Quantity);
                    ci.ProductVariant.SoldCount += ci.Quantity;
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, orderId = order.OrderId, orderCode = order.OrderCode, total });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message;
                var msg = string.IsNullOrWhiteSpace(inner) ? ex.Message : ($"{ex.Message} | {inner}");
                return StatusCode(500, new { success = false, message = msg });
            }
        }

        private async Task EnsurePaymentSchemaAsync()
        {
            var script = @"
DECLARE @fk NVARCHAR(128);
SELECT TOP 1 @fk = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables t ON fk.parent_object_id = t.object_id
JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = fkc.parent_column_id
WHERE t.name = 'Orders' AND c.name = 'PaymentId';

IF @fk IS NOT NULL EXEC('ALTER TABLE Orders DROP CONSTRAINT [' + @fk + ']');

DECLARE @idx NVARCHAR(128);
SELECT TOP 1 @idx = i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name = 'Orders' AND c.name = 'PaymentId';

IF @idx IS NOT NULL EXEC('DROP INDEX [' + @idx + '] ON Orders');

IF COL_LENGTH('Orders','PaymentId') IS NOT NULL
BEGIN
    ALTER TABLE Orders DROP COLUMN PaymentId;
END

IF OBJECT_ID('PaymentMethods','U') IS NOT NULL
BEGIN
    DROP TABLE PaymentMethods;
END

IF COL_LENGTH('Orders','PaymentType') IS NULL
BEGIN
    ALTER TABLE Orders ADD PaymentType nvarchar(20) NOT NULL DEFAULT 'COD';
END
";
            await _context.Database.ExecuteSqlRawAsync(script);
            await _context.Database.MigrateAsync();
        }
    }
}
