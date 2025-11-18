using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;
using ZENITH.Models;
using ZENITH.AppData;

namespace ZENITH.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private string ResolveAvatar(string? path)
        {
            var defaultUrl = Url.Content("~/image/account/default-avatar.jpg");
            if (string.IsNullOrWhiteSpace(path)) return defaultUrl;

            var s = path.Trim().Replace('\\', '/');
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return s;

            string webPath;
            if (s.StartsWith("~/")) webPath = s.Substring(2);
            else if (s.StartsWith("/")) webPath = s.Substring(1);
            else
            {
                var idx = s.ToLowerInvariant().IndexOf("wwwroot");
                if (idx >= 0)
                {
                    var tail = s.Substring(idx + "wwwroot".Length);
                    webPath = tail.TrimStart('/');
                }
                else
                {
                    webPath = s.TrimStart('/');
                }
            }

            var phys = Path.Combine(_env.WebRootPath, webPath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(phys)) return defaultUrl;

            return Url.Content("~/" + webPath);
        }
        private string ResolveImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Url.Content("~/image/default.avif");
            var s = path.Trim().Replace('\\', '/');
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
            return Url.Content("~/" + s.TrimStart('/'));
        }

        public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);
            var defaultAddress = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsDefault);
            if (defaultAddress == null)
            {
                defaultAddress = await _context.Addresses
                    .AsNoTracking()
                    .Where(a => a.UserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.AddressId)
                    .FirstOrDefaultAsync();
            }
            if (defaultAddress != null)
            {
                var parts = new[] { defaultAddress.AddressLine, defaultAddress.Ward, defaultAddress.District, defaultAddress.City };
                var s = string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
                ViewBag.DefaultAddress = s;
                ViewBag.DefaultAddressId = defaultAddress.AddressId;
            }
            else
            {
                ViewBag.DefaultAddress = string.Empty;
                ViewBag.DefaultAddressId = 0;
            }
            var favorites = await _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == user.Id)
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

            for (int i = 0; i < favorites.Count; i++)
            {
                favorites[i].ImageUrl = ResolveImageUrl(favorites[i].ImageUrl);
            }
            ViewBag.Favorites = favorites;
            ViewBag.PaymentMethods = null;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { success = false, message = "Không có tệp ảnh được chọn." });
            }

            var contentType = avatar.ContentType.ToLowerInvariant();
            if (!contentType.StartsWith("image/"))
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp hình ảnh hợp lệ." });
            }

            var accountDir = Path.Combine(_env.WebRootPath, "image", "account");
            Directory.CreateDirectory(accountDir);

            var ext = Path.GetExtension(avatar.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var safeExt = ext.Length <= 5 ? ext : ".jpg";
            var fileName = $"avatar_{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{safeExt}";
            var savePath = Path.Combine(accountDir, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            var relativeUrl = Url.Content($"~/image/account/{fileName}");
            user.Avatar = relativeUrl;
            await _userManager.UpdateAsync(user);

            return Ok(new { success = true, avatarUrl = relativeUrl });
        }
        public async Task<IActionResult> EditPersonalInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);

            return View();
        }
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> EditAddressInfo(int? addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);

            Address? addr = null;
            if (addressId.HasValue && addressId.Value > 0)
            {
                addr = await _context.Addresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AddressId == addressId.Value && a.UserId == user.Id);
            }
            if (addr == null)
            {
                addr = await _context.Addresses
                    .AsNoTracking()
                    .Where(a => a.UserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.AddressId)
                    .FirstOrDefaultAsync();
            }
            ViewBag.AddressId = addr?.AddressId ?? 0;
            ViewBag.AddressLine = addr?.AddressLine ?? string.Empty;
            ViewBag.City = addr?.City ?? string.Empty;
            ViewBag.District = addr?.District ?? string.Empty;
            ViewBag.Ward = addr?.Ward ?? string.Empty;
            ViewBag.IsDefault = addr?.IsDefault ?? false;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPersonalInfo(string fullName, string email, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            user.FullName = fullName?.Trim() ?? user.FullName;
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                await _userManager.SetPhoneNumberAsync(user, phoneNumber.Trim());
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                await _userManager.SetEmailAsync(user, email.Trim());
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index), new { t = DateTime.UtcNow.Ticks });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddressInfo(int? addressId, string addressLine, string city, string district, string ward, bool isDefault)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            Models.Address? addr = null;
            if (addressId.HasValue && addressId.Value > 0)
            {
                addr = _context.Addresses.FirstOrDefault(a => a.AddressId == addressId.Value && a.UserId == user.Id);
            }

            if (addr == null)
            {
                addr = new Models.Address
                {
                    UserId = user.Id
                };
                _context.Addresses.Add(addr);
            }

            addr.FullName = user.FullName;
            addr.Phone = user.PhoneNumber ?? string.Empty;
            addr.AddressLine = string.IsNullOrWhiteSpace(addressLine) ? string.Empty : addressLine.Trim();
            addr.City = string.IsNullOrWhiteSpace(city) ? string.Empty : city.Trim();
            addr.District = string.IsNullOrWhiteSpace(district) ? string.Empty : district.Trim();
            addr.Ward = string.IsNullOrWhiteSpace(ward) ? string.Empty : ward.Trim();
            addr.IsDefault = true;
            var others = _context.Addresses.Where(a => a.UserId == user.Id && a.AddressId != addr.AddressId);
            foreach (var o in others)
            {
                o.IsDefault = false;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { t = DateTime.UtcNow.Ticks });
        }
        public async Task<IActionResult> EditPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return await EditPassword();
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, e.Description);
                }
                return await EditPassword();
            }

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.PhoneNumber = user.PhoneNumber ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var requirePassword = await _userManager.HasPasswordAsync(user);
            if (requirePassword && !await _userManager.CheckPasswordAsync(user, password))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác.");
                return await DeleteAccount();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return await DeleteAccount();
            }

            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
        
    }
}
