using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using ZENITH.Models;
using ZENITH.AppData;

namespace ZENITH.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            string ResolveAvatar(string? path)
            {
                var defaultUrl = Url.Content("~/image/account/default-avatar.jpg");
                if (string.IsNullOrWhiteSpace(path)) return defaultUrl;

                var s = path.Trim().Replace('\\', '/');
                // External URL: return as is
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return s;

                // Normalize to web path under wwwroot
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

                // Check physical file existence
                var phys = Path.Combine(_env.WebRootPath, webPath.Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(phys)) return defaultUrl;

                return Url.Content("~/" + webPath);
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email ?? string.Empty;
            ViewBag.RegisteredDate = user.CreatedAt;
            ViewBag.AvatarUrl = ResolveAvatar(user.Avatar);

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
    }
}
