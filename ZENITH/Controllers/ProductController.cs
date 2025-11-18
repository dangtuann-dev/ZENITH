using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZENITH.AppData;
using ZENITH.ViewModels;
using ZENITH.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;

namespace ZENITH.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<Product> BaseQuery()
        {
            return _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages.Where(i => i.IsPrimary))
                .Include(p => p.Reviews)
                .Include(p => p.Category)
                .Include(p => p.Sport)
                .Where(p => p.IsActive)
                .AsNoTracking();
        }

        private static List<ProductCardViewModel> MapToCards(List<Product> products)
        {
            return products.Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                SkuBase = p.Sku,
                SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                Price = p.ProductVariants.OrderByDescending(v => v.Price).FirstOrDefault()?.Price ?? 0,
                SalePrice = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.SalePrice ?? 0,
                VariantId = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.VariantId ??
                            p.ProductVariants.FirstOrDefault()?.VariantId ?? 0,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl ?? "~/image/default.webp",
                Rating = p.Reviews != null && p.Reviews.Any() ? (double)p.Reviews.Average(r => r.Rating) : 0
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? categoryId, int? sportId, string? sort)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var text = q.Trim();
                query = query.Where(p => p.ProductName.Contains(text) || p.Description.Contains(text) || p.Sku.Contains(text));
            }
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var cid = categoryId.Value;
                query = query.Where(p => p.CategoryId == cid || (p.Category != null && p.Category.ParentCategoryId == cid));
            }
            if (sportId.HasValue && sportId.Value > 0)
            {
                var sid = sportId.Value;
                query = query.Where(p => p.SportId == sid || (p.Sport != null && p.Sport.ParentSportId == sid));
            }

            var products = await query
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.CreatedAt)
                .Take(200)
                .ToListAsync();

            var vm = new ProductListViewModel
            {
                Query = q,
                Sort = sort,
                CategoryId = categoryId,
                SportId = sportId,
                TotalCount = products.Count,
                Products = MapToCards(products),
                Categories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new CategoryItem { Id = c.CategoryId, Name = c.CategoryName })
                    .AsNoTracking().ToListAsync(),
                Sports = await _context.Sports
                    .Where(s => s.IsActive && s.ParentSportId == null)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new SportItem { Id = s.SportId, Name = s.SportName })
                    .AsNoTracking().ToListAsync(),
            };

            // Build sports tree (parent -> subSports -> categories)
            var parentSports = await _context.Sports
                .Where(s => s.IsActive && s.ParentSportId == null)
                .OrderBy(s => s.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            var subSports = await _context.Sports
                .Where(s => s.IsActive && s.ParentSportId != null)
                .OrderBy(s => s.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            var sportCats = await _context.SportCategories
                .Include(sc => sc.Category)
                .AsNoTracking()
                .ToListAsync();

            var subByParent = subSports.GroupBy(s => s.ParentSportId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var catsBySport = sportCats.GroupBy(sc => sc.SportId)
                .ToDictionary(g => g.Key, g => g.Select(x => new CategoryItem { Id = x.CategoryId, Name = x.Category.CategoryName }).DistinctBy(c => c.Id).OrderBy(c => c.Name).ToList());

            vm.SportsTree = parentSports.Select(ps => new SportNode
            {
                Id = ps.SportId,
                Name = ps.SportName,
                Children = (subByParent.TryGetValue(ps.SportId, out var children) ? children : new List<Models.Sport>())
                    .Select(ch => new SportNode
                    {
                        Id = ch.SportId,
                        Name = ch.SportName,
                        Categories = catsBySport.TryGetValue(ch.SportId, out var catList) ? catList : new List<CategoryItem>()
                    }).ToList()
            }).ToList();

            // Đánh dấu yêu thích nếu người dùng đăng nhập
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var favIds = await _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.VariantId)
                        .ToListAsync();
                    var set = favIds.ToHashSet();
                    foreach (var p in vm.Products)
                    {
                        p.IsUserFavorite = set.Contains(p.VariantId);
                    }
                }
            }

            // Apply sort to view model list
            vm.Products = sort switch
            {
                "price_asc" => vm.Products.OrderBy(p => p.SalePrice).ThenBy(p => p.Price).ToList(),
                "price_desc" => vm.Products.OrderByDescending(p => p.SalePrice).ThenByDescending(p => p.Price).ToList(),
                "name_asc" => vm.Products.OrderBy(p => p.ProductName).ToList(),
                "name_desc" => vm.Products.OrderByDescending(p => p.ProductName).ToList(),
                _ => vm.Products
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Build images list (primary first then by display order)
            var images = product.ProductImages
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .ToList();
            if (!images.Any())
            {
                images.Add("~/image/default.avif");
            }

            // Choose a default variant (prefer one with SalePrice)
            var defaultVariant = product.ProductVariants
                .OrderBy(v => v.SalePrice.HasValue ? 0 : 1)
                .ThenByDescending(v => v.Price)
                .FirstOrDefault();

            // Map variants
            var variantOptions = product.ProductVariants
                .OrderBy(v => v.VariantId)
                .Select(v => new VariantOptionViewModel
                {
                    VariantId = v.VariantId,
                    Text = BuildVariantText(v),
                    Price = v.Price,
                    SalePrice = v.SalePrice,
                    StockQuantity = v.StockQuantity,
                    IsSelected = defaultVariant != null && v.VariantId == defaultVariant.VariantId
                })
                .ToList();

            // Build attribute groups (unique values across variants)
            var allAttributeValues = product.ProductVariants
                .SelectMany(v => v.VariantAttributeValues)
                .Select(vav => vav.AttributeValue)
                .ToList();

            List<AttributeGroupViewModel> attributeGroups;

            if (allAttributeValues.Any())
            {
                // Preferred: use normalized VariantAttributeValues
                attributeGroups = allAttributeValues
                    .GroupBy(av => av.AttributeId)
                    .OrderBy(g => g.First().Attribute.DisplayOrder)
                    .Select(g => new AttributeGroupViewModel
                    {
                        AttributeId = g.Key,
                        AttributeName = g.First().Attribute.AttributeName,
                        DisplayName = g.First().Attribute.DisplayName,
                        InputType = g.First().Attribute.InputType,
                        Options = g
                            .GroupBy(x => x.ValueId)
                            .OrderBy(gg => gg.First().DisplayOrder)
                            .Select(gg => new AttributeValueOptionViewModel
                            {
                                ValueId = gg.Key,
                                ValueName = gg.First().ValueName,
                                ColorCode = gg.First().ColorCode,
                                IsAvailable = product.ProductVariants.Any(v => v.VariantAttributeValues.Any(vav => vav.ValueId == gg.Key)),
                                IsSelected = defaultVariant != null && defaultVariant.VariantAttributeValues.Any(vav => vav.ValueId == gg.Key)
                            })
                            .ToList()
                    })
                    .ToList();
            }
            else
            {
                // Fallback: parse from ProductVariant.Attributes column (e.g., "Size: L, Color: Red")
                var parsed = new List<(string AttrName, string Value)>();
                foreach (var v in product.ProductVariants)
                {
                    var attrs = v.Attributes;
                    if (string.IsNullOrWhiteSpace(attrs)) continue;
                    var parts = attrs.Split(',');
                    foreach (var part in parts)
                    {
                        var seg = part.Trim();
                        var idx = seg.IndexOf(':');
                        if (idx <= 0) continue;
                        var name = seg.Substring(0, idx).Trim();
                        var value = seg.Substring(idx + 1).Trim();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            parsed.Add((name, value));
                        }
                    }
                }

                attributeGroups = parsed
                    .GroupBy(x => x.AttrName)
                    .OrderBy(g => g.Key)
                    .Select(g => new AttributeGroupViewModel
                    {
                        AttributeId = 0, // unknown id in fallback mode
                        AttributeName = g.Key,
                        DisplayName = g.Key,
                        InputType = "select",
                        Options = g
                            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
                            .OrderBy(gg => gg.Key)
                            .Select(gg => new AttributeValueOptionViewModel
                            {
                                // generate a stable pseudo id from name+value to allow selection rendering
                                ValueId = Math.Abs(string.Concat(g.Key, ":", gg.Key).GetHashCode()),
                                ValueName = gg.Key,
                                ColorCode = null,
                                IsAvailable = true,
                                IsSelected = defaultVariant != null && !string.IsNullOrWhiteSpace(defaultVariant.Attributes) &&
                                             defaultVariant.Attributes.IndexOf($"{g.Key}:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                             defaultVariant.Attributes.IndexOf(gg.Key, StringComparison.OrdinalIgnoreCase) >= 0
                            })
                            .ToList()
                    })
                    .ToList();
            }

            // Reviews summary
            var reviewCount = product.Reviews.Count;
            double avgRating = 0;
            if (reviewCount > 0)
            {
                avgRating = (double)product.Reviews.Average(r => r.Rating);
            }

            var vm = new ProductDetailViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Sku = product.Sku,
                Description = product.Description,
                SupplierName = product.Supplier?.SupplierName,
                CategoryName = product.Category?.CategoryName,
                ImageUrls = images,
                SelectedVariantId = defaultVariant?.VariantId,
                Variants = variantOptions,
                AttributeGroups = attributeGroups,
                Price = defaultVariant?.Price ?? product.ProductVariants.OrderBy(v => v.Price).FirstOrDefault()?.Price ?? 0,
                SalePrice = defaultVariant?.SalePrice,
                StockQuantity = defaultVariant?.StockQuantity ?? 0,
                ReviewCount = reviewCount,
                AverageRating = avgRating
            };

            var currentVariantId = vm.SelectedVariantId ?? vm.Variants.FirstOrDefault()?.VariantId ?? 0;
            if (currentVariantId > 0 && User?.Identity?.IsAuthenticated == true)
            {
                var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(uid))
                {
                    vm.IsUserFavorite = await _context.Favorites.AnyAsync(f => f.UserId == uid && f.VariantId == currentVariantId);
                    var myReview = await _context.Reviews.Where(r => r.ProductId == product.ProductId && r.UserId == uid).FirstOrDefaultAsync();
                    if (myReview != null)
                    {
                        vm.MyReviewId = myReview.ReviewId;
                        vm.MyReview = new ReviewItemViewModel
                        {
                            UserFullName = product.Reviews.FirstOrDefault(r => r.UserId == uid)?.User?.FullName ?? "Bạn",
                            AvatarUrl = ResolveAvatar(product.Reviews.FirstOrDefault(r => r.UserId == uid)?.User?.Avatar),
                            Rating = (double)myReview.Rating,
                            Comment = myReview.Comment ?? string.Empty,
                            CreatedAt = myReview.CreatedAt,
                            IsVerifiedPurchase = myReview.IsVerifiedPurchase
                        };
                    }
                }
            }

            vm.Reviews = product.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewItemViewModel
                {
                    UserFullName = r.User != null ? r.User.FullName : "Người dùng",
                    AvatarUrl = ResolveAvatar(r.User?.Avatar),
                    Rating = (double)r.Rating,
                    Comment = r.Comment ?? string.Empty,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedPurchase = r.IsVerifiedPurchase
                })
                .ToList();

            // Similar products: same sport, highest views, exclude current
            var sportId = product.SportId;
            if (sportId.HasValue)
            {
                var similar = await _context.Products
                    .Include(p => p.Supplier)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages.Where(i => i.IsPrimary))
                    .Where(p => p.IsActive && p.ProductId != product.ProductId && p.SportId == sportId)
                    .OrderByDescending(p => p.ViewCount)
                    .Take(12)
                    .AsNoTracking()
                    .ToListAsync();

                vm.SimilarProducts = similar.Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    SkuBase = p.Sku,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    Price = p.ProductVariants.OrderByDescending(v => v.Price).FirstOrDefault()?.Price ?? 0,
                    SalePrice = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.SalePrice ?? 0,
                    VariantId = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.VariantId ??
                                p.ProductVariants.FirstOrDefault()?.VariantId ?? 0,
                    ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl ?? "~/image/default.webp",
                    Rating = p.Reviews.Any() ? (double)p.Reviews.Average(r => r.Rating) : 0
                }).ToList();
            }

            return View(vm);
        }

        public class ResolveVariantRequest
        {
            public int ProductId { get; set; }
            public List<int> ValueIds { get; set; } = new List<int>();
        }

        [HttpPost]
        public async Task<IActionResult> ResolveVariant([FromBody] ResolveVariantRequest req)
        {
            if (req == null || req.ProductId <= 0 || req.ValueIds == null || req.ValueIds.Count == 0)
            {
                return BadRequest(new { success = false, message = "Thiếu thông tin." });
            }

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == req.ProductId && v.IsActive)
                .Include(v => v.VariantAttributeValues)
                .ToListAsync();

            var match = variants.FirstOrDefault(v =>
                v.VariantAttributeValues != null &&
                v.VariantAttributeValues.Count == req.ValueIds.Count &&
                req.ValueIds.All(id => v.VariantAttributeValues.Any(x => x.ValueId == id))
            );

            if (match == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy biến thể phù hợp." });
            }

            return Ok(new
            {
                success = true,
                variantId = match.VariantId,
                price = match.Price,
                salePrice = match.SalePrice,
                stockQuantity = match.StockQuantity
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, decimal rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            var verified = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.ProductVariant)
                .AnyAsync(oi => oi.Order.UserId == userId && oi.ProductVariant.ProductId == productId && oi.Order.PaymentStatus == "Paid");

            var rounded = Math.Round(rating * 2m, MidpointRounding.AwayFromZero) / 2m;
            if (rounded < 0.5m) rounded = 0.5m;
            if (rounded > 5m) rounded = 5m;

            var existing = await _context.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
            if (existing != null)
            {
                existing.Rating = rounded;
                existing.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
                existing.IsApproved = true;
                existing.IsVerifiedPurchase = verified;
            }
            else
            {
                var review = new Review
                {
                    UserId = userId,
                    ProductId = productId,
                    Rating = rounded,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    IsApproved = true,
                    IsVerifiedPurchase = verified,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(review);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = productId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var existing = await _context.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
            if (existing != null)
            {
                _context.Reviews.Remove(existing);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Detail), new { id = productId });
        }

        private string ResolveAvatar(string? path)
        {
            var d = Url.Content("~/image/account/default-avatar.jpg");
            if (string.IsNullOrWhiteSpace(path)) return d;
            var s = path.Trim();
            if (s.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)) return s;
            if (s.StartsWith("~/")) return Url.Content(s);
            if (s.StartsWith("/")) return s;
            return Url.Content("~/" + s.TrimStart('/'));
        }

        private static string BuildVariantText(ProductVariant v)
        {
            // Compose attribute text like "Size: L, Color: Red"; fallback to VariantSku
            var parts = v.VariantAttributeValues
                .OrderBy(x => x.AttributeValue.Attribute.DisplayOrder)
                .Select(x => $"{x.AttributeValue.Attribute.DisplayName}: {x.AttributeValue.ValueName}")
                .ToList();
            if (parts.Count == 0)
            {
                return v.Attributes ?? v.VariantSku;
            }
            return string.Join(", ", parts);
        }
    }
}
