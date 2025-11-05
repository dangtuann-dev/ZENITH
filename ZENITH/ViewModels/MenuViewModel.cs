namespace ZENITH.ViewModels
{
    // Mô hình cho Category
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Mô hình cho mỗi mục Sport trên menu
    public class SportMenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        // Mối quan hệ N-M: Categories liên quan đến Sport này
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        // Mối quan hệ Cha-Con: Các Sport con
        public List<SportMenuItem> SubSports { get; set; } = new List<SportMenuItem>();
    }

    // Mô hình chính để truyền dữ liệu từ Controller sang View
    public class MenuViewModel
    {
        public List<SportMenuItem> TopLevelSports { get; set; } = new List<SportMenuItem>();
    }
}