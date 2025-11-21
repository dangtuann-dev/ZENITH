using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZENITH.AppData;
using ZENITH.ViewModels;
using ZENITH.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZENITH.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private async Task<AdminDashboardViewModel> BuildDashboardViewModel()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var last30 = now.AddDays(-30);

            var totalUsers = await _context.Users.CountAsync();
            var newUsers7Days = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-7));

            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.IsActive);

            var lowStockVariants = await _context.ProductVariants.CountAsync(v => v.StockQuantity <= v.LowStockThreshold);

            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending");
            var shippedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipped");
            var deliveredOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Delivered");

            var paidOrders = _context.Orders.Where(o => o.PaymentStatus == "Paid");
            var revenueToday = await paidOrders.Where(o => o.OrderDate >= todayStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var revenue30Days = await paidOrders.Where(o => o.OrderDate >= last30).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var avgOrderValue = await paidOrders.AverageAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new RecentOrderItem
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    CustomerName = o.User.FullName,
                    TotalAmount = o.TotalAmount,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            var topProducts = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.ProductVariants.Sum(v => v.SoldCount))
                .Take(10)
                .Select(p => new TopProductItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    TotalSold = p.ProductVariants.Sum(v => v.SoldCount),
                    VariantsCount = p.ProductVariants.Count
                })
                .ToListAsync();

            var lowStocks = await _context.ProductVariants
                .AsNoTracking()
                .Where(v => v.StockQuantity <= v.LowStockThreshold)
                .OrderBy(v => v.StockQuantity)
                .Take(20)
                .Select(v => new LowStockItem
                {
                    VariantId = v.VariantId,
                    ProductName = v.Product.ProductName,
                    VariantSku = v.VariantSku,
                    StockQuantity = v.StockQuantity,
                    LowStockThreshold = v.LowStockThreshold
                })
                .ToListAsync();

            var pendingReviews = await _context.Reviews
                .AsNoTracking()
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new PendingReviewItem
                {
                    ReviewId = r.ReviewId,
                    ProductName = r.Product.ProductName,
                    UserEmail = r.User.Email,
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                NewUsers7Days = newUsers7Days,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                LowStockVariants = lowStockVariants,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                ReviewsPending = pendingReviews.Count,
                RevenueToday = revenueToday,
                Revenue30Days = revenue30Days,
                AverageOrderValue = avgOrderValue,
                RecentOrders = recentOrders,
                TopProducts = topProducts,
                LowStocks = lowStocks,
                PendingReviews = pendingReviews
            };
            return vm;
        }

        public async Task<IActionResult> Index()
        {
            var vm = await BuildDashboardViewModel();
            Response.Cookies.Append("admin_seen_dashboard", DateTime.UtcNow.ToString("O"), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
            return View(vm);
        }

        public async Task<IActionResult> Dashboard()
        {
            var vm = await BuildDashboardViewModel();
            return View("Index", vm);
        }
        [HttpGet]
        [Route("Admin/Metrics")]
        public async Task<IActionResult> Metrics()
        {
            var today = DateTime.Today;
            var days = Enumerable.Range(0, 14).Select(i => today.AddDays(-13 + i)).ToList();
            var labels = days.Select(d => d.ToString("dd/MM")).ToList();

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= days.First() && o.OrderDate < days.Last().AddDays(1))
                .Select(o => new { o.OrderDate, o.UserId, o.TotalAmount, o.OrderStatus })
                .ToListAsync();

            var visitors = new List<int>();
            var sales = new List<decimal>();
            foreach (var d in days)
            {
                var dayOrders = orders.Where(o => o.OrderDate.Date == d.Date);
                visitors.Add(dayOrders.Select(o => o.UserId).Distinct().Count());
                sales.Add(dayOrders.Sum(o => o.TotalAmount));
            }

            return Ok(new { labels, visitors, sales });
        }
        public async Task<IActionResult> ProductManagement(int? categoryId, int? sportId, string? query, string? sort)
        {
            var baseQ = _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages.Where(i => i.IsPrimary))
                .Include(p => p.Reviews)
                .Include(p => p.Category)
                .Include(p => p.Sport)
                .AsNoTracking();

            if (sportId.HasValue) baseQ = baseQ.Where(p => p.CategoryId == categoryId);
            if (sportId.HasValue) baseQ = baseQ.Where(p => p.SportId == sportId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQ = baseQ.Where(p => p.ProductName.Contains(q) || p.Sku.Contains(q));
            }

            var products = await baseQ.ToListAsync();
            var list = products.Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                SkuBase = p.Sku,
                SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                Price = p.ProductVariants.OrderByDescending(v => v.Price).FirstOrDefault()?.Price ?? 0,
                SalePrice = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.SalePrice ?? 0,
                VariantId = p.ProductVariants.OrderBy(v => v.SalePrice).FirstOrDefault()?.VariantId ?? p.ProductVariants.FirstOrDefault()?.VariantId ?? 0,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl ?? "~/image/default.avif",
                Rating = p.Reviews != null && p.Reviews.Any() ? (double)p.Reviews.Average(r => r.Rating) : 0
            }).ToList();

            list = (sort ?? "").ToLowerInvariant() switch
            {
                "price_asc" => list.OrderBy(p => p.SalePrice).ThenBy(p => p.Price).ToList(),
                "price_desc" => list.OrderByDescending(p => p.SalePrice).ThenByDescending(p => p.Price).ToList(),
                "name_asc" => list.OrderBy(p => p.ProductName).ToList(),
                "name_desc" => list.OrderByDescending(p => p.ProductName).ToList(),
                _ => list
            };

            var catQuery = _context.Categories.AsNoTracking();
            if (sportId.HasValue && sportId.Value > 0)
            {
                var sid = sportId.Value;
                catQuery = catQuery.Where(c => _context.SportCategories.Any(sc => sc.CategoryId == c.CategoryId && (sc.SportId == sid || _context.Sports.Any(s => s.ParentSportId == sid && s.SportId == sc.SportId))));
            }
            var categories = await catQuery.OrderBy(c => c.DisplayOrder).ToListAsync();
            var sports = await _context.Sports.AsNoTracking().OrderBy(s => s.SportId).ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.Sports = sports;
            ViewBag.CategoryList = new SelectList(categories, nameof(Category.CategoryId), nameof(Category.CategoryName), categoryId);
            ViewBag.SportList = new SelectList(sports, nameof(Sport.SportId), nameof(Sport.SportName), sportId);
            ViewBag.Query = query;
            ViewBag.Sort = sort;

            Response.Cookies.Append("admin_seen_products", DateTime.UtcNow.ToString("O"), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
            return View(list);
        }
        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ToListAsync();
            var sports = await _context.Sports.AsNoTracking().OrderBy(s => s.SportId).ToListAsync();
            var suppliers = await _context.Suppliers.AsNoTracking().OrderBy(s => s.SupplierId).ToListAsync();
            ViewBag.CreateCategoryList = new SelectList(categories, nameof(Category.CategoryId), nameof(Category.CategoryName));
            ViewBag.CreateSportList = new SelectList(sports, nameof(Sport.SportId), nameof(Sport.SportName));
            ViewBag.CreateSupplierList = new SelectList(suppliers, nameof(Supplier.SupplierId), nameof(Supplier.SupplierName));
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct([Bind("ProductName,Description,CategoryId,SupplierId,SportId")] Product input, decimal? price, decimal? salePrice, string? primaryImageUrl, string? extraImageUrls, Microsoft.AspNetCore.Http.IFormFile? primaryImageFile, System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile>? extraImageFiles)
        {
            var now = DateTime.UtcNow;
            var product = new Product
            {
                ProductName = input.ProductName,
                Description = input.Description,
                CategoryId = input.CategoryId,
                SupplierId = input.SupplierId,
                SportId = input.SportId,
                Sku = ($"SKU-{now:yyyyMMddHHmmss}").ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.ProductId,
                VariantSku = product.Sku + "-VAR1",
                Price = price ?? 0,
                SalePrice = salePrice,
                StockQuantity = 0,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };
            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            int displayOrder = 0;
            string baseFolderRel = $"image/productImage/{product.Sku}";
            string baseFolder = System.IO.Path.Combine(_env.WebRootPath, baseFolderRel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!System.IO.Directory.Exists(baseFolder)) System.IO.Directory.CreateDirectory(baseFolder);

            if (primaryImageFile != null && primaryImageFile.Length > 0)
            {
                var ext = System.IO.Path.GetExtension(primaryImageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(ext)) ext = ".webp";
                var fname = $"{product.Sku}-1{ext}";
                var phys = System.IO.Path.Combine(baseFolder, fname);
                using (var fs = System.IO.File.Create(phys)) { await primaryImageFile.CopyToAsync(fs); }
                var url = $"~/image/productImage/{product.Sku}/{fname}";
                _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = url, IsPrimary = true, DisplayOrder = displayOrder++ });
            }
            else if (!string.IsNullOrWhiteSpace(primaryImageUrl))
            {
                _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = primaryImageUrl.Trim(), IsPrimary = true, DisplayOrder = displayOrder++ });
            }

            if (extraImageFiles != null && extraImageFiles.Count > 0)
            {
                int idx = 2;
                foreach (var f in extraImageFiles)
                {
                    if (f == null || f.Length == 0) continue;
                    var ext = System.IO.Path.GetExtension(f.FileName).ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".webp";
                    var fname = $"{product.Sku}-{idx}{ext}";
                    var phys = System.IO.Path.Combine(baseFolder, fname);
                    using (var fs = System.IO.File.Create(phys)) { await f.CopyToAsync(fs); }
                    var url = $"~/image/productImage/{product.Sku}/{fname}";
                    _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = url, IsPrimary = false, DisplayOrder = displayOrder++ });
                    idx++;
                }
            }
            else if (!string.IsNullOrWhiteSpace(extraImageUrls))
            {
                var parts = extraImageUrls.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var s = p.Trim(); if (string.IsNullOrWhiteSpace(s)) continue;
                    _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = s, IsPrimary = false, DisplayOrder = displayOrder++ });
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ProductManagement));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductImage(int productId, string imageUrl, bool isPrimary)
        {
            var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();
            if (isPrimary)
            {
                foreach (var img in product.ProductImages) img.IsPrimary = false;
            }
            int nextOrder = (product.ProductImages.Any() ? product.ProductImages.Max(i => i.DisplayOrder) + 1 : 0);
            _context.ProductImages.Add(new ProductImage { ProductId = productId, ImageUrl = imageUrl.Trim(), IsPrimary = isPrimary, DisplayOrder = nextOrder });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(EditProduct), new { id = productId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductImage(int imageId, string imageUrl, bool isPrimary)
        {
            var img = await _context.ProductImages.FirstOrDefaultAsync(i => i.ImageId == imageId);
            if (img == null) return NotFound();
            img.ImageUrl = imageUrl.Trim();
            if (isPrimary)
            {
                var siblings = _context.ProductImages.Where(i => i.ProductId == img.ProductId && i.ImageId != img.ImageId);
                await siblings.ForEachAsync(i => i.IsPrimary = false);
                img.IsPrimary = true;
            }
            else
            {
                img.IsPrimary = false;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(EditProduct), new { id = img.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplaceProductImage(int imageId, Microsoft.AspNetCore.Http.IFormFile file)
        {
            var img = await _context.ProductImages.FirstOrDefaultAsync(i => i.ImageId == imageId);
            if (img == null) return NotFound();
            var rel = img.ImageUrl.StartsWith("~/") ? img.ImageUrl.Substring(2) : img.ImageUrl.TrimStart('/');
            var phys = System.IO.Path.Combine(_env.WebRootPath, rel.Replace('/', System.IO.Path.DirectorySeparatorChar));
            var dir = System.IO.Path.GetDirectoryName(phys);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            using (var fs = System.IO.File.Create(phys)) { await file.CopyToAsync(fs); }
            return RedirectToAction(nameof(EditProduct), new { id = img.ProductId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            var img = await _context.ProductImages.FirstOrDefaultAsync(i => i.ImageId == imageId);
            if (img == null) return NotFound();
            int pid = img.ProductId;
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(EditProduct), new { id = pid });
        }
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Supplier)
                .Include(p => p.Category)
                .Include(p => p.Sport)
                .Include(p => p.ProductVariants)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();
            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ToListAsync();
            var sports = await _context.Sports.AsNoTracking().OrderBy(s => s.SportId).ToListAsync();
            ViewBag.EditCategoryList = new SelectList(categories, nameof(Category.CategoryId), nameof(Category.CategoryName), product.CategoryId);
            ViewBag.EditSportList = new SelectList(sports, nameof(Sport.SportId), nameof(Sport.SportName), product.SportId);
            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, [Bind("ProductId,ProductName,Description,CategoryId,SupplierId,SportId")] Product input, decimal? price, decimal? salePrice)
        {
            var product = await _context.Products.Include(p => p.ProductVariants).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();
            product.ProductName = input.ProductName;
            product.Description = input.Description;
            product.CategoryId = input.CategoryId;
            product.SupplierId = input.SupplierId;
            product.SportId = input.SportId;
            product.UpdatedAt = DateTime.UtcNow;
            var variant = product.ProductVariants.OrderBy(v => v.VariantId).FirstOrDefault();
            if (variant != null)
            {
                if (price.HasValue) variant.Price = price.Value;
                if (salePrice.HasValue) variant.SalePrice = salePrice.Value;
                variant.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ProductManagement));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ProductManagement));
        }
        public async Task<IActionResult> UserManagement(string? query)
        {
            var baseQ = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQ = baseQ.Where(u => u.FullName.Contains(q) || u.Email.Contains(q));
            }
            var users = await baseQ.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Query = query;
            Response.Cookies.Append("admin_seen_users", DateTime.UtcNow.ToString("O"), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
            return View(users);
        }
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, [Bind("Id,FullName,Email")] ApplicationUser input)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            user.FullName = input.FullName;
            user.Email = input.Email;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(UserManagement));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            bool hasRefs =
                await _context.Orders.AnyAsync(o => o.UserId == id) ||
                await _context.Reviews.AnyAsync(r => r.UserId == id) ||
                await _context.CartItems.AnyAsync(c => c.UserId == id) ||
                await _context.Favorites.AnyAsync(f => f.UserId == id) ||
                await _context.InventoryLogs.AnyAsync(il => il.UserId == id) ||
                await _context.OrderStatusHistories.AnyAsync(h => h.UserId == id);
            if (hasRefs)
            {
                TempData["Error"] = "Không thể xoá người dùng vì có dữ liệu liên quan";
                return RedirectToAction(nameof(UserManagement));
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(UserManagement));
        }

        [HttpGet]
        public async Task<IActionResult> OrderManagement(string? status, string? query)
        {
            var statuses = new[] { "Chờ xác nhận", "Vận chuyển", "Chờ Giao Hàng", "Hoàn Thành" };
            ViewBag.Statuses = statuses;
            ViewBag.SelectedStatus = status;
            ViewBag.Query = query;

            var baseQ = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
            {
                baseQ = baseQ.Where(o => o.OrderStatus == status);
            }
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQ = baseQ.Where(o => o.OrderCode.Contains(q) || o.User.FullName.Contains(q) || o.User.Email.Contains(q));
            }

            var orders = await baseQ.OrderByDescending(o => o.OrderDate).ToListAsync();
            Response.Cookies.Append("admin_seen_orders", DateTime.UtcNow.ToString("O"), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();
            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderManagement), new { status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int orderId, string status, string? filterStatus, string? query)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();
            order.PaymentStatus = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderManagement), new { status = filterStatus, query });
        }
    }
}
