using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZENITH.AppData;
using ZENITH.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ZENITH.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private const string SessionCartKey = "cart.items";
        private const string SessionAddressKey = "checkout.address";

        public CheckoutController(ApplicationDbContext context, UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }
                
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<Models.CartItemSession> cartItemsRaw;
                if (string.IsNullOrEmpty(sCart)) cartItemsRaw = new List<Models.CartItemSession>();
                else { try { cartItemsRaw = JsonSerializer.Deserialize<List<Models.CartItemSession>>(sCart) ?? new List<Models.CartItemSession>(); } catch { cartItemsRaw = new List<Models.CartItemSession>(); } }

                cartItemsRaw = cartItemsRaw
                    .Where(x => x != null && x.VariantId > 0)
                    .GroupBy(x => x.VariantId)
                    .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                    .ToList();

                var variantIds = cartItemsRaw.Select(x => x.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => variantIds.Contains(v.VariantId))
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .ToListAsync();

                static string BuildVariantTextGuest(Models.ProductVariant v)
                {
                    if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                    if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                    return $"SKU {v.VariantId}";
                }

                var cultureG = new System.Globalization.CultureInfo("vi-VN");
                
                // Filter out invalid variants that no longer exist
                var validCartItems = cartItemsRaw
                    .Where(ci => variants.Any(v => v.VariantId == ci.VariantId && v.Product != null))
                    .ToList();
                
                int itemCountG = validCartItems.Sum(c => Math.Max(c.Quantity,1));
                decimal subtotalG = validCartItems.Sum(c => (variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * Math.Max(c.Quantity,1));
                decimal shippingG = validCartItems.Count * 15000m;
                decimal totalG = subtotalG + shippingG;

                var itemsG = validCartItems.Select(ci =>
                {
                    var v = variants.FirstOrDefault(x => x.VariantId == ci.VariantId);
                    if (v == null || v.Product == null) return null;
                    return new CheckoutItemViewModel
                    {
                        VariantId = ci.VariantId,
                        ProductId = v.ProductId,
                        ProductName = v.Product.ProductName ?? string.Empty,
                        ImageUrl = ResolveImageUrl(v.Product.ProductImages.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()),
                        Quantity = Math.Max(ci.Quantity,1),
                        UnitPrice = v.SalePrice ?? v.Price,
                        LineTotal = ((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity,1)),
                        AttributesText = BuildVariantTextGuest(v),
                        StockQuantity = v.StockQuantity,
                        UnitPriceFormatted = ((v.SalePrice ?? v.Price)).ToString("N0", cultureG) + " VND",
                        LineTotalFormatted = (((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity,1))).ToString("N0", cultureG) + " VND"
                    };
                })
                .Where(item => item != null)
                .ToList();

                foreach (var it in itemsG)
                {
                    var allVariants = await _context.ProductVariants
                        .AsNoTracking()
                        .Where(v => v.ProductId == it.ProductId && v.IsActive)
                        .OrderBy(v => v.SalePrice ?? v.Price)
                        .ToListAsync();
                    it.Variants = allVariants.Select(v => new ZENITH.ViewModels.VariantOptionViewModel
                    {
                        VariantId = v.VariantId,
                        Text = BuildVariantTextGuest(v),
                        Price = v.Price,
                        SalePrice = v.SalePrice,
                        StockQuantity = v.StockQuantity,
                        IsSelected = v.VariantId == it.VariantId
                    }).ToList();
                }

                var modelG = new CheckoutIndexViewModel
                {
                    Items = itemsG,
                    ItemCount = itemCountG,
                    Subtotal = subtotalG,
                    Shipping = shippingG,
                    Total = totalG,
                    SubtotalFormatted = subtotalG.ToString("N0", cultureG) + " VND",
                    ShippingFormatted = shippingG.ToString("N0", cultureG) + " VND",
                    TotalFormatted = totalG.ToString("N0", cultureG) + " VND"
                };

                return View(modelG);
            }

            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                
                .ToListAsync();

            string BuildVariantText(Models.ProductVariant v)
            {
                if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                return $"SKU {v.VariantId}";
            }

            var culture = new System.Globalization.CultureInfo("vi-VN");
            var merged = cartItems
                .GroupBy(c => c.VariantId)
                .Select(g => new
                {
                    Variant = g.First().ProductVariant,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            int itemCount = merged.Sum(m => m.Quantity);
            decimal subtotal = merged.Sum(m => (m.Variant.SalePrice ?? m.Variant.Price) * m.Quantity);
            decimal shipping = merged.Count * 15000m;
            decimal total = subtotal + shipping;

            var items = merged.Select(m => new CheckoutItemViewModel
            {
                VariantId = m.Variant.VariantId,
                ProductId = m.Variant.ProductId,
                ProductName = m.Variant.Product.ProductName,
                ImageUrl = ResolveImageUrl(m.Variant.Product.ProductImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()),
                Quantity = m.Quantity,
                UnitPrice = m.Variant.SalePrice ?? m.Variant.Price,
                LineTotal = (m.Variant.SalePrice ?? m.Variant.Price) * m.Quantity,
                AttributesText = BuildVariantText(m.Variant),
                StockQuantity = m.Variant.StockQuantity,
                UnitPriceFormatted = (m.Variant.SalePrice ?? m.Variant.Price).ToString("N0", culture) + " VND",
                LineTotalFormatted = (((m.Variant.SalePrice ?? m.Variant.Price) * m.Quantity)).ToString("N0", culture) + " VND"
            }).ToList();

            foreach (var it in items)
            {
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == it.ProductId && v.IsActive)
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
            if (string.IsNullOrEmpty(userId))
            {
                int variantIdG = request?.VariantId ?? 0;
                int deltaG = request?.Delta ?? 0;
                if (variantIdG <= 0 || deltaG == 0) return BadRequest(new { success = false });
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<(int VariantId,int Quantity)> cartRaw;
                if (string.IsNullOrEmpty(sCart)) cartRaw = new List<(int,int)>();
                else { try { cartRaw = JsonSerializer.Deserialize<List<(int VariantId,int Quantity)>>(sCart) ?? new List<(int,int)>(); } catch { cartRaw = new List<(int,int)>(); } }
                var idx = cartRaw.FindIndex(x => x.VariantId == variantIdG);
                if (idx >= 0)
                {
                    var next = cartRaw[idx].Quantity + deltaG;
                    if (next <= 0) cartRaw.RemoveAt(idx);
                    else cartRaw[idx] = (variantIdG, next);
                }
                HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cartRaw));
                return Ok(new { success = true });
            }
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
            if (string.IsNullOrEmpty(userId))
            {
                int variantIdG = request?.VariantId ?? 0;
                if (variantIdG <= 0) return BadRequest(new { success = false });
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<(int VariantId,int Quantity)> cartRaw;
                if (string.IsNullOrEmpty(sCart)) cartRaw = new List<(int,int)>();
                else { try { cartRaw = JsonSerializer.Deserialize<List<(int VariantId,int Quantity)>>(sCart) ?? new List<(int,int)>(); } catch { cartRaw = new List<(int,int)>(); } }
                cartRaw = cartRaw.Where(x => x.VariantId != variantIdG).ToList();
                HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cartRaw));
                return Ok(new { success = true });
            }
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

            // Handle guest users (not logged in) - save to session
            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }

                var addrVm = new AddressItemViewModel
                {
                    AddressId = 0, // Guest addresses don't have database IDs
                    FullName = fullName,
                    Phone = phone,
                    AddressLine = addressLine,
                    Ward = ward,
                    District = district,
                    City = city,
                    IsDefault = true
                };

                HttpContext.Session.SetString(SessionAddressKey, JsonSerializer.Serialize(addrVm));

                return Ok(new
                {
                    success = true,
                    address = new
                    {
                        addressId = 0,
                        fullName = addrVm.FullName,
                        phone = addrVm.Phone,
                        addressLine = addrVm.AddressLine,
                        ward = addrVm.Ward,
                        district = addrVm.District,
                        city = addrVm.City,
                        isDefault = addrVm.IsDefault,
                        displayText = addrVm.DisplayText
                    }
                });
            }

            // Handle logged-in users - save to database
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
            int id = request?.AddressId ?? 0;
            
            // Handle guest users (not logged in) - delete from session
            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }
                
                // For guest users, addressId is always 0, just clear the session
                HttpContext.Session.Remove(SessionAddressKey);
                return Ok(new { success = true });
            }
            
            // Handle logged-in users - delete from database
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
            if (string.IsNullOrEmpty(userId))
            {
                int oldIdG = request?.OldVariantId ?? 0;
                int newIdG = request?.NewVariantId ?? 0;
                if (oldIdG <= 0 || newIdG <= 0 || oldIdG == newIdG) return BadRequest(new { success = false });
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<(int VariantId,int Quantity)> cartRaw;
                if (string.IsNullOrEmpty(sCart)) cartRaw = new List<(int,int)>();
                else { try { cartRaw = JsonSerializer.Deserialize<List<(int VariantId,int Quantity)>>(sCart) ?? new List<(int,int)>(); } catch { cartRaw = new List<(int,int)>(); } }
                var idxOld = cartRaw.FindIndex(x => x.VariantId == oldIdG);
                if (idxOld < 0) return NotFound(new { success = false });
                var qty = cartRaw[idxOld].Quantity;
                cartRaw.RemoveAt(idxOld);
                var idxNew = cartRaw.FindIndex(x => x.VariantId == newIdG);
                if (idxNew >= 0) cartRaw[idxNew] = (newIdG, cartRaw[idxNew].Quantity + qty);
                else cartRaw.Add((newIdG, qty));
                HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(cartRaw));
                return Ok(new { success = true });
            }
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
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                System.Collections.Generic.List<Models.CartItemSession> cartItemsRaw;
                if (string.IsNullOrEmpty(sCart)) cartItemsRaw = new System.Collections.Generic.List<Models.CartItemSession>();
                else { try { cartItemsRaw = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<Models.CartItemSession>>(sCart) ?? new System.Collections.Generic.List<Models.CartItemSession>(); } catch { cartItemsRaw = new System.Collections.Generic.List<Models.CartItemSession>(); } }

                cartItemsRaw = cartItemsRaw
                    .Where(x => x != null && x.VariantId > 0)
                    .GroupBy(x => x.VariantId)
                    .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                    .ToList();
                var variantIds = cartItemsRaw.Select(x => x.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => variantIds.Contains(v.VariantId))
                    .ToListAsync();
                int countG = cartItemsRaw.Sum(c => Math.Max(c.Quantity,1));
                decimal subtotalG = cartItemsRaw.Sum(c => (variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * Math.Max(c.Quantity,1));
                decimal shippingG = cartItemsRaw.Count * 15000m;
                decimal taxG = 0m;
                decimal totalG = subtotalG + shippingG + taxG;
                return Ok(new
                {
                    count = countG,
                    subtotal = subtotalG,
                    shipping = shippingG,
                    tax = taxG,
                    total = totalG,
                    subtotalFormatted = subtotalG.ToString("N0", culture) + " VND",
                    shippingFormatted = shippingG.ToString("N0", culture) + " VND",
                    totalFormatted = totalG.ToString("N0", culture) + " VND"
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
        public async Task<IActionResult> Shipping()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }
                
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<Models.CartItemSession> cartItemsRaw;
                if (string.IsNullOrEmpty(sCart)) cartItemsRaw = new List<Models.CartItemSession>();
                else { try { cartItemsRaw = JsonSerializer.Deserialize<List<Models.CartItemSession>>(sCart) ?? new List<Models.CartItemSession>(); } catch { cartItemsRaw = new List<Models.CartItemSession>(); } }

                cartItemsRaw = cartItemsRaw
                    .Where(x => x != null && x.VariantId > 0)
                    .GroupBy(x => x.VariantId)
                    .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                    .ToList();
                
                var variantIds = cartItemsRaw.Select(x => x.VariantId).ToList();
                var variants = _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => variantIds.Contains(v.VariantId))
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .ToList();

                static string BuildVariantTextGuest(Models.ProductVariant v)
                {
                    if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                    if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                    return $"SKU {v.VariantId}";
                }
                var cultureG = new System.Globalization.CultureInfo("vi-VN");
                
                // Filter out invalid variants that no longer exist
                var validCartItems = cartItemsRaw
                    .Where(ci => variants.Any(v => v.VariantId == ci.VariantId && v.Product != null))
                    .ToList();
                
                int itemCountG = validCartItems.Sum(c => Math.Max(c.Quantity, 1));
                int lineCountG = validCartItems.Count;
                decimal subtotalG = validCartItems.Sum(c => (variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * Math.Max(c.Quantity, 1));
                decimal shippingG = lineCountG * 15000m;
                decimal totalG = subtotalG + shippingG;
                var itemsG = validCartItems.Select(ci =>
                {
                    var v = variants.FirstOrDefault(x => x.VariantId == ci.VariantId);
                    if (v == null || v.Product == null) return null;
                    return new CheckoutItemViewModel
                    {
                        VariantId = ci.VariantId,
                        ProductId = v.ProductId,
                        ProductName = v.Product.ProductName ?? string.Empty,
                        ImageUrl = ResolveImageUrl(v.Product.ProductImages.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()),
                        Quantity = Math.Max(ci.Quantity, 1),
                        UnitPrice = v.SalePrice ?? v.Price,
                        LineTotal = ((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity, 1)),
                        AttributesText = BuildVariantTextGuest(v),
                        StockQuantity = v.StockQuantity,
                        UnitPriceFormatted = ((v.SalePrice ?? v.Price)).ToString("N0", cultureG) + " VND",
                        LineTotalFormatted = (((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity, 1))).ToString("N0", cultureG) + " VND"
                    };
                })
                .Where(item => item != null)
                .ToList();
                AddressItemViewModel? addr = null;
                var sAddr = HttpContext.Session.GetString(SessionAddressKey);
                if (!string.IsNullOrEmpty(sAddr)) { try { addr = JsonSerializer.Deserialize<AddressItemViewModel>(sAddr); } catch { addr = null; } }
                var modelG = new ShippingViewModel
                {
                    Addresses = addr != null ? new List<AddressItemViewModel> { addr } : new List<AddressItemViewModel>(),
                    SelectedAddressId = 0,
                    Items = itemsG,
                    ItemCount = itemCountG,
                    Subtotal = subtotalG,
                    Shipping = shippingG,
                    Total = totalG,
                    SubtotalFormatted = subtotalG.ToString("N0", cultureG) + " VND",
                    ShippingFormatted = shippingG.ToString("N0", cultureG) + " VND",
                    TotalFormatted = totalG.ToString("N0", cultureG) + " VND"
                };
                return View(modelG);
            }

            var cartItems = _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                
                .ToList();



            string BuildVariantText(Models.ProductVariant v)
            {
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
        public async Task<IActionResult> Payment(int? addressId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                // Ensure session is available
                if (!HttpContext.Session.IsAvailable)
                {
                    await HttpContext.Session.LoadAsync();
                }
                
                var sCart = HttpContext.Session.GetString(SessionCartKey);
                List<Models.CartItemSession> cartItemsRaw;
                if (string.IsNullOrEmpty(sCart)) cartItemsRaw = new List<Models.CartItemSession>();
                else { try { cartItemsRaw = JsonSerializer.Deserialize<List<Models.CartItemSession>>(sCart) ?? new List<Models.CartItemSession>(); } catch { cartItemsRaw = new List<Models.CartItemSession>(); } }

                cartItemsRaw = cartItemsRaw
                    .Where(x => x != null && x.VariantId > 0)
                    .GroupBy(x => x.VariantId)
                    .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                    .ToList();
                
                var variantIds = cartItemsRaw.Select(x => x.VariantId).ToList();
                var variants = _context.ProductVariants
                    .AsNoTracking()
                    .Where(v => variantIds.Contains(v.VariantId))
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .ToList();

                static string BuildVariantTextGuest(Models.ProductVariant v)
                {
                    if (!string.IsNullOrWhiteSpace(v.Attributes)) return v.Attributes.Trim();
                    if (!string.IsNullOrWhiteSpace(v.VariantSku)) return v.VariantSku;
                    return $"SKU {v.VariantId}";
                }
                var cultureG = new System.Globalization.CultureInfo("vi-VN");
                
                // Filter out invalid variants that no longer exist
                var validCartItems = cartItemsRaw
                    .Where(ci => variants.Any(v => v.VariantId == ci.VariantId && v.Product != null))
                    .ToList();
                
                int itemCountG = validCartItems.Sum(c => Math.Max(c.Quantity, 1));
                int lineCountG = validCartItems.Count;
                decimal subtotalG = validCartItems.Sum(c => (variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * Math.Max(c.Quantity, 1));
                decimal shippingG = lineCountG * 15000m;
                decimal totalG = subtotalG + shippingG;
                var itemsG = validCartItems.Select(ci =>
                {
                    var v = variants.FirstOrDefault(x => x.VariantId == ci.VariantId);
                    if (v == null || v.Product == null) return null;
                    return new CheckoutItemViewModel
                    {
                        VariantId = ci.VariantId,
                        ProductId = v.ProductId,
                        ProductName = v.Product.ProductName ?? string.Empty,
                        ImageUrl = ResolveImageUrl(v.Product.ProductImages.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()),
                        Quantity = Math.Max(ci.Quantity, 1),
                        UnitPrice = v.SalePrice ?? v.Price,
                        LineTotal = ((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity, 1)),
                        AttributesText = BuildVariantTextGuest(v),
                        StockQuantity = v.StockQuantity,
                        UnitPriceFormatted = ((v.SalePrice ?? v.Price)).ToString("N0", cultureG) + " VND",
                        LineTotalFormatted = (((v.SalePrice ?? v.Price) * Math.Max(ci.Quantity, 1))).ToString("N0", cultureG) + " VND"
                    };
                })
                .Where(item => item != null)
                .ToList();
                AddressItemViewModel? addr = null;
                var sAddr = HttpContext.Session.GetString(SessionAddressKey);
                if (!string.IsNullOrEmpty(sAddr)) { try { addr = JsonSerializer.Deserialize<AddressItemViewModel>(sAddr); } catch { addr = null; } }
                var modelG = new PaymentViewModel
                {
                    SelectedAddress = addr,
                    Items = itemsG,
                    ItemCount = itemCountG,
                    Subtotal = subtotalG,
                    Shipping = shippingG,
                    Total = totalG,
                    SubtotalFormatted = subtotalG.ToString("N0", cultureG) + " VND",
                    ShippingFormatted = shippingG.ToString("N0", cultureG) + " VND",
                    TotalFormatted = totalG.ToString("N0", cultureG) + " VND"
                };
                return View(modelG);
            }

            var cartItems = _context.CartItems
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                
                .ToList();



            string BuildVariantText(Models.ProductVariant v)
            {
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
                ZENITH.Models.ApplicationUser? user = null;

                if (string.IsNullOrEmpty(userId))
                {
                    AddressItemViewModel? addrVm = null;
                    {
                        var sAddr = HttpContext.Session.GetString(SessionAddressKey);
                        if (!string.IsNullOrEmpty(sAddr))
                        {
                            try { addrVm = JsonSerializer.Deserialize<AddressItemViewModel>(sAddr); } catch { addrVm = null; }
                        }
                    }
                    if (addrVm == null) return BadRequest(new { success = false, message = "Vui lòng nhập địa chỉ giao hàng." });

                    var email = ($"guest-{Guid.NewGuid():N}@example.local").ToLowerInvariant();
                    var guestUser = new ZENITH.Models.ApplicationUser { UserName = email, Email = email, FullName = addrVm.FullName, PhoneNumber = addrVm.Phone };
                    var pwd = $"G{Guid.NewGuid():N}!9a";
                    var create = await _userManager.CreateAsync(guestUser, pwd);
                    if (!create.Succeeded) return StatusCode(500, new { success = false, message = "Không thể tạo tài khoản khách." });
                    userId = guestUser.Id;
                    user = guestUser;

                    List<Models.CartItemSession> cartRaw;
                    {
                        var sCart = HttpContext.Session.GetString(SessionCartKey);
                        if (string.IsNullOrEmpty(sCart)) cartRaw = new List<Models.CartItemSession>();
                        else { try { cartRaw = JsonSerializer.Deserialize<List<Models.CartItemSession>>(sCart) ?? new List<Models.CartItemSession>(); } catch { cartRaw = new List<Models.CartItemSession>(); } }
                    }
                    
                    cartRaw = cartRaw
                        .Where(x => x != null && x.VariantId > 0)
                        .GroupBy(x => x.VariantId)
                        .Select(g => new Models.CartItemSession { VariantId = g.Key, Quantity = Math.Max(1, g.Sum(i => i.Quantity)) })
                        .ToList();
                    
                    if (cartRaw.Count == 0) return BadRequest(new { success = false, message = "Giỏ hàng trống." });
                    var variantIdsG = cartRaw.Select(x => x.VariantId).ToList();
                    var variantsG = await _context.ProductVariants
                        .Where(v => variantIdsG.Contains(v.VariantId) && v.IsActive)
                        .ToListAsync();
                    
                    // Filter out invalid variants that no longer exist
                    var validCartItemsG = cartRaw
                        .Where(ci => variantsG.Any(v => v.VariantId == ci.VariantId))
                        .ToList();
                    
                    if (validCartItemsG.Count == 0) return BadRequest(new { success = false, message = "Không có sản phẩm hợp lệ trong giỏ hàng." });

                    string methodG = (request?.ShippingMethod ?? "standard").ToLowerInvariant();
                    decimal perLineG = methodG == "express" ? 30000m : 15000m;
                    if (request?.ShippingRate is decimal srG && srG > 0) perLineG = srG;
                    decimal subtotalG = validCartItemsG.Sum(c => (variantsG.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variantsG.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * Math.Max(c.Quantity, 1));
                    int lineCountG = validCartItemsG.Count;
                    decimal shippingFeeG = perLineG * lineCountG;
                    decimal totalG = subtotalG + shippingFeeG;

                    var addrEntity = new ZENITH.Models.Address
                    {
                        UserId = userId,
                        FullName = addrVm.FullName ?? string.Empty,
                        Phone = addrVm.Phone ?? string.Empty,
                        AddressLine = addrVm.AddressLine ?? string.Empty,
                        Ward = addrVm.Ward ?? string.Empty,
                        District = addrVm.District ?? string.Empty,
                        City = addrVm.City ?? string.Empty,
                        IsDefault = true
                    };
                    _context.Addresses.Add(addrEntity);
                    await _context.SaveChangesAsync();

                    string ptypeG = (request?.PaymentType ?? "cod").ToUpperInvariant();
                    string orderCodeG = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0,6)}";
                    var orderG = new ZENITH.Models.Order
                    {
                        UserId = userId,
                        AddressId = addrEntity.AddressId,
                        PaymentType = ptypeG,
                        OrderCode = orderCodeG,
                        Subtotal = subtotalG,
                        ShippingFee = shippingFeeG,
                        Discount = 0,
                        TotalAmount = totalG,
                        PaymentStatus = "Pending",
                        OrderStatus = "Processing",
                        Note = null,
                        OrderDate = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Orders.Add(orderG);
                    await _context.SaveChangesAsync();

                    foreach (var ci in validCartItemsG)
                    {
                        var v = variantsG.FirstOrDefault(x => x.VariantId == ci.VariantId);
                        if (v == null) continue; // Skip invalid variants
                        var unit = (v.SalePrice ?? v.Price);
                        _context.OrderItems.Add(new ZENITH.Models.OrderItem
                        {
                            OrderId = orderG.OrderId,
                            VariantId = ci.VariantId,
                            Quantity = Math.Max(ci.Quantity, 1),
                            UnitPrice = unit,
                            TotalPrice = unit * Math.Max(ci.Quantity, 1),
                            VariantDescription = !string.IsNullOrWhiteSpace(v.Attributes) ? v.Attributes.Trim() : (!string.IsNullOrWhiteSpace(v.VariantSku) ? v.VariantSku : $"SKU {v.VariantId}")
                        });
                        v.StockQuantity = Math.Max(0, v.StockQuantity - Math.Max(ci.Quantity, 1));
                        v.SoldCount += Math.Max(ci.Quantity, 1);
                    }
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString(SessionCartKey, JsonSerializer.Serialize(new List<Models.CartItemSession>()));
                    return Ok(new { success = true, orderId = orderG.OrderId, orderCode = orderG.OrderCode, total = totalG });
                }

                user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized(new { success = false, message = "Not logged in" });
                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                if (cartItems.Count == 0) return BadRequest(new { success = false, message = "Cart is empty" });

                var variantIds = cartItems.Select(ci => ci.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .Where(v => variantIds.Contains(v.VariantId))
                    .ToListAsync();

                int? addressId = request?.AddressId;
                var address = addressId.HasValue
                    ? await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId)
                    : await _context.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.AddressId).FirstOrDefaultAsync(a => a.UserId == userId);
                if (address == null) return BadRequest(new { success = false, message = "Address not found" });

                string method = (request?.ShippingMethod ?? "standard").ToLowerInvariant();
                decimal perLine = method == "express" ? 30000m : 15000m;
                if (request?.ShippingRate is decimal sr && sr > 0) perLine = sr;
                decimal subtotal = cartItems.Sum(c => (variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.SalePrice ?? variants.FirstOrDefault(v => v.VariantId == c.VariantId)?.Price ?? 0) * c.Quantity);
                int lineCount = cartItems.Count;
                decimal shippingFee = perLine * lineCount;
                decimal total = subtotal + shippingFee;

                string ptype = (request?.PaymentType ?? "cod").ToUpperInvariant();
                string orderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0,6)}";
                var order = new ZENITH.Models.Order
                {
                    UserId = userId,
                    AddressId = address.AddressId,
                    PaymentType = ptype,
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
                await _context.SaveChangesAsync();

                foreach (var ci in cartItems)
                {
                    var v = variants.FirstOrDefault(x => x.VariantId == ci.VariantId) ?? await _context.ProductVariants.FirstOrDefaultAsync(x => x.VariantId == ci.VariantId);
                    var unit = (v?.SalePrice ?? v?.Price ?? 0);
                    _context.OrderItems.Add(new ZENITH.Models.OrderItem
                    {
                        OrderId = order.OrderId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        UnitPrice = unit,
                        TotalPrice = unit * ci.Quantity,
                        VariantDescription = v != null ? (!string.IsNullOrWhiteSpace(v.Attributes) ? v.Attributes.Trim() : (!string.IsNullOrWhiteSpace(v.VariantSku) ? v.VariantSku : $"SKU {v.VariantId}")) : $"SKU {ci.VariantId}"
                    });
                    if (v != null)
                    {
                        v.StockQuantity = Math.Max(0, v.StockQuantity - ci.Quantity);
                        v.SoldCount += ci.Quantity;
                    }
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

        private string ResolveImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
            var s = path.Trim();
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
            if (s.StartsWith("~/")) return Url.Content(s);
            if (s.StartsWith("/")) return Url.Content(s);
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
    }
}
