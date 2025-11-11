namespace ZENITH.ViewModels
{
    public class VariantOptionViewModel
    {
        public int VariantId { get; set; }
        public string Text { get; set; } = string.Empty; // Ví dụ: "Size: L, Color: Red"
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsSelected { get; set; }
    }
}
