// Trong thư mục ViewComponents/
using Microsoft.AspNetCore.Mvc;
using ZENITH.Services; // Namespace chứa IMenuService
using ZENITH.ViewModels; // 

[ViewComponent(Name = "Menu")]
public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    // Dependency Injection: Nhận MenuService
    public MenuViewComponent(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Gọi service để lấy dữ liệu menu đã được tổ chức sẵn
        var menuData = await _menuService.GetMenuDataAsync();

        // Trả về dữ liệu cho View Component
        return View(new MenuViewModel { TopLevelSports = menuData });
    }
}