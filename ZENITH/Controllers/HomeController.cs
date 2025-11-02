using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ZENITH.AppData;


namespace ZENITH.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) // DI hoạt động là CSDL được tìm thấy
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Kiểm tra kết nối CSDL và đảm bảo các bảng Identity đã được tạo.
                // Nếu CSDL không tồn tại, lệnh này sẽ tạo nó (dựa trên Migration).
                // Nếu có lỗi kết nối, nó sẽ báo lỗi ngay lập tức.
                await _context.Database.EnsureCreatedAsync();

                // Lấy số lượng Categories (để kiểm tra xem truy vấn có hoạt động không)
                int categoryCount = await _context.Categories.CountAsync();

                ViewBag.Status = "KẾT NỐI THÀNH CÔNG!";
                ViewBag.Message = $"CSDL đã sẵn sàng. Số lượng danh mục: {categoryCount}";

                // Trả về một View đơn giản để hiển thị trạng thái
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Status = "KẾT NỐI THẤT BẠI!";
                ViewBag.Message = $"Lỗi: {ex.Message}";
                return View();
            }
        }




    }
}
