using System.Collections.Generic;

namespace ZENITH.ViewModels
{
    public class ProductListViewModel
    {
        public string? Query { get; set; }
        public string? Sort { get; set; }
        public int? CategoryId { get; set; }
        public int? SportId { get; set; }
        public int TotalCount { get; set; }
        public List<ProductCardViewModel> Products { get; set; } = new List<ProductCardViewModel>();

        public List<CategoryItem> Categories { get; set; } = new List<CategoryItem>();
        public List<SportItem> Sports { get; set; } = new List<SportItem>();
        public List<SportNode> SportsTree { get; set; } = new List<SportNode>();
    }

    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SportItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SportNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SportNode> Children { get; set; } = new List<SportNode>();
        public List<CategoryItem> Categories { get; set; } = new List<CategoryItem>();
    }
}
