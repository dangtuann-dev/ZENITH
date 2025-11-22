using ZENITH.Models;

namespace ZENITH.ViewModels
{
    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SkuBase { get; set; } = string.Empty;
        public int FavoriteCount { get; set; }  public int VariantId { get; set; }
        public decimal Price { get; set; } 
        public decimal SalePrice { get; set; } 
        public string SupplierName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; 
        public double Rating { get; set; } 
        public bool IsUserFavorite { get; set; }
        
    }

    public class HomeIndexViewModel
    {
        public List<ZENITH.Models.Category> Categories { get; set; } = new List<ZENITH.Models.Category>();
        public int TotalCount { get; set; }
        public List<ProductCardViewModel> TopSellingProducts { get; set; } = new List<ProductCardViewModel>();
        public List<ProductCardViewModel> RacketSportsProducts { get; set; } = new List<ProductCardViewModel>();
         public List<ProductCardViewModel> ClimbingProducts { get; set; } = new List<ProductCardViewModel>();
        public List<ProductCardViewModel> TeamSportsProducts { get; set; } = new List<ProductCardViewModel>();
        public List<ProductCardViewModel> FootwearCollection { get; set; } = new List<ProductCardViewModel>();
    }
}