using ZENITH.Models;

namespace ZENITH.ViewModels
{
    public class ProductCardViewModel
    {
        // Thông tin từ bảng Product
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SkuBase { get; set; } = string.Empty;

        // 💡 THUỘC TÍNH MỚI BỊ THIẾU: Dùng ProductId cho hành động Favorite
        public int FavoriteCount { get; set; } // Lượt yêu thích của sản phẩm (tổng từ Variants)

        // Thông tin từ bảng ProductVariants (Giả định lấy từ Variant đầu tiên)
        // ⚠️ GIẢ ĐỊNH: VariantId là ID của biến thể đầu tiên/chính
        public int VariantId { get; set; }
        public decimal Price { get; set; } // Giá niêm yết
        public decimal SalePrice { get; set; } // Giá bán (dùng để hiển thị)

        // Thông tin từ các bảng khác
        public string SupplierName { get; set; } = string.Empty; // Brand/Supplier
        public string ImageUrl { get; set; } = string.Empty; // Ảnh chính (Primary Image)
        public double Rating { get; set; } // Điểm đánh giá trung bình

        // Cần thêm trường này nếu bạn muốn đánh dấu sản phẩm đã được user hiện tại yêu thích hay chưa
        public bool IsUserFavorite { get; set; }
        
    }

    public class HomeIndexViewModel
    {
        public List<ZENITH.Models.Category> Categories { get; set; } = new List<ZENITH.Models.Category>();
        public int TotalCount { get; set; }

        // ROW 1: SẢN PHẨM BÁN CHẠY (15 sp)
        public List<ProductCardViewModel> TopSellingProducts { get; set; } = new List<ProductCardViewModel>();

        // ROW 2: DANH MỤC CÁC MÔN THỂ THAO PHỔ BIẾN
        // Thường sử dụng lại thuộc tính Categories hoặc List<Sport> nếu bạn có Sport Model

        // ROW 3: SẢN PHẨM DÙNG VỢT
        public List<ProductCardViewModel> RacketSportsProducts { get; set; } = new List<ProductCardViewModel>();

        // ROW 4: SẢN PHẨM LEO NÚI
        public List<ProductCardViewModel> ClimbingProducts { get; set; } = new List<ProductCardViewModel>();

        // (Tùy chọn) ROW 5: SẢN PHẨM THỂ THAO ĐỒNG ĐỘI
        public List<ProductCardViewModel> TeamSportsProducts { get; set; } = new List<ProductCardViewModel>();
        public List<ProductCardViewModel> FootwearCollection { get; set; } = new List<ProductCardViewModel>();
    }
}