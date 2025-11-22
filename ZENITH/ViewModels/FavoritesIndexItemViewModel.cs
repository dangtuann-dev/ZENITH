using System;
using System.Globalization;

namespace ZENITH.ViewModels
{
    public class FavoritesIndexItemViewModel
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }

        // Danh sách các biến thể của sản phẩm để hiển thị lựa chọn
        public List<VariantOptionViewModel> Variants { get; set; } = new List<VariantOptionViewModel>();
        public string PriceFormatted => FormatCurrency(Price);
        public string? SalePriceFormatted => SalePrice.HasValue ? FormatCurrency(SalePrice.Value) : null;
        public string StockStatusText => StockQuantity > 0 ? "Còn hàng" : "Hết hàng";

        private static string FormatCurrency(decimal value)
        {
           
            var culture = new CultureInfo("vi-VN");
            return string.Format(culture, "{0:C0}", value);
        }
    }
}
