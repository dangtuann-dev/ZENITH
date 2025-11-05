using Microsoft.AspNetCore.Mvc;
using ZENITH.Services;
using ZENITH.ViewModels;
using System.Threading.Tasks;

namespace ZENITH.ViewComponents
{
    [ViewComponent(Name = "AccessoryMenu")]
    public class AccessoryMenuViewComponent : ViewComponent
    {
        private readonly IMenuService _menuService;

        public AccessoryMenuViewComponent(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 💡 GỌI HÀM MỚI để lấy danh sách phẳng của Sports con/Categories
            var menuItems = await _menuService.GetAllSubSportsWithCategoriesAsync();

            // ViewModel cho AccessoryMenu chỉ cần chứa danh sách SportMenuItem
            var viewModel = new MenuViewModel
            {
                TopLevelSports = menuItems
            };

            // Trả về view Default.cshtml
            return View(viewModel);
        }
    }
}