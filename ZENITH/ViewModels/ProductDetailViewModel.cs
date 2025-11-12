using System.Globalization;

namespace ZENITH.ViewModels
{
    public class ProductDetailViewModel
    {
        // Core product info
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Relations
        public string? SupplierName { get; set; }
        public string? CategoryName { get; set; }

        // Images
        public List<string> ImageUrls { get; set; } = new List<string>();

        // Variants
        public int? SelectedVariantId { get; set; }
        public List<VariantOptionViewModel> Variants { get; set; } = new List<VariantOptionViewModel>();
        public List<AttributeGroupViewModel> AttributeGroups { get; set; } = new List<AttributeGroupViewModel>();

        // Similar products
        public List<ProductCardViewModel> SimilarProducts { get; set; } = new List<ProductCardViewModel>();

        // Pricing & stock
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }

        // Reviews summary
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public List<ReviewItemViewModel> Reviews { get; set; } = new List<ReviewItemViewModel>();

        public bool IsUserFavorite { get; set; }

        public ReviewItemViewModel? MyReview { get; set; }
        public int? MyReviewId { get; set; }

        // Convenience formatting
        public string PriceFormatted => FormatCurrency(Price);
        public string? SalePriceFormatted => SalePrice.HasValue ? FormatCurrency(SalePrice.Value) : null;

        private static string FormatCurrency(decimal value)
        {
            var culture = new CultureInfo("vi-VN");
            return string.Format(culture, "{0:N0} VND", value);
        }
    }

    public class ReviewItemViewModel
    {
        public string UserFullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }
}
