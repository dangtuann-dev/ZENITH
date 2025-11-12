using System.Collections.Generic;

namespace ZENITH.ViewModels
{
    public class CheckoutItemViewModel
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string UnitPriceFormatted { get; set; } = string.Empty;
        public string LineTotalFormatted { get; set; } = string.Empty;
        public string AttributesText { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public List<VariantOptionViewModel> Variants { get; set; } = new List<VariantOptionViewModel>();
    }

    public class CheckoutIndexViewModel
    {
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public string SubtotalFormatted { get; set; } = string.Empty;
        public string ShippingFormatted { get; set; } = string.Empty;
        public string TotalFormatted { get; set; } = string.Empty;
    }

    public class AddressItemViewModel
    {
        public int AddressId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public string DisplayText => string.Join(", ", new[] { AddressLine, Ward, District, City }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    public class ShippingViewModel
    {
        public List<AddressItemViewModel> Addresses { get; set; } = new List<AddressItemViewModel>();
        public int? SelectedAddressId { get; set; }
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public string SubtotalFormatted { get; set; } = string.Empty;
        public string ShippingFormatted { get; set; } = string.Empty;
        public string TotalFormatted { get; set; } = string.Empty;
    }

    public class PaymentViewModel
    {
        public AddressItemViewModel? SelectedAddress { get; set; }
        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
        public int ItemCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public string SubtotalFormatted { get; set; } = string.Empty;
        public string ShippingFormatted { get; set; } = string.Empty;
        public string TotalFormatted { get; set; } = string.Empty;
    }
}
