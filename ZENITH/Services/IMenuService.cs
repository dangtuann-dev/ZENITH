using ZENITH.ViewModels;

namespace ZENITH.Services
{
    public interface IMenuService
    {
        // Lấy dữ liệu menu đã được cấu trúc
        Task<List<SportMenuItem>> GetMenuDataAsync();
    }
}