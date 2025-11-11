using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ZENITH.AppData;
using ZENITH.ViewModels;
using ZENITH.Models;

namespace ZENITH.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
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
                avgRating = product.Reviews.Average(r => r.Rating);
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
                    Rating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 4.5
                }).ToList();
            }

            return View(vm);
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
