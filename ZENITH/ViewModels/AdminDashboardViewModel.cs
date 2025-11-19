using System;
using System.Collections.Generic;

namespace ZENITH.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int NewUsers7Days { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockVariants { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int ReviewsPending { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal Revenue30Days { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<RecentOrderItem> RecentOrders { get; set; } = new List<RecentOrderItem>();
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();
        public List<LowStockItem> LowStocks { get; set; } = new List<LowStockItem>();
        public List<PendingReviewItem> PendingReviews { get; set; } = new List<PendingReviewItem>();
    }

    public class RecentOrderItem
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    public class TopProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public int VariantsCount { get; set; }
    }

    public class LowStockItem
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string VariantSku { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class PendingReviewItem
    {
        public int ReviewId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
