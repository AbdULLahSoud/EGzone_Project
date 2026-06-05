using System;
using System.Collections.Generic;

namespace EGzone1.Dto.Dashboard
{
    public class DashboardResponseDto
    {
        public OverviewDto Overview { get; set; } = new();
        public RevenueStatsDto RevenueStats { get; set; } = new();
        public OrderStatsDto OrderStats { get; set; } = new();
        public ProductStatsDto ProductStats { get; set; } = new();
        public ReviewStatsDto ReviewStats { get; set; } = new();
        public List<SalesChartDto> SalesChart { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    }

    public class OverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
    }

    public class RevenueStatsDto
    {
        public decimal Today { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal ThisYear { get; set; }
    }

    public class OrderStatsDto
    {
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Shipped { get; set; }
        public int Delivered { get; set; }
        public int Cancelled { get; set; }
    }

    public class ProductStatsDto
    {
        public int ActiveProducts { get; set; }
        public int OutOfStock { get; set; }
        public int LowStock { get; set; }
    }

    public class ReviewStatsDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStars { get; set; }
        public int FourStars { get; set; }
        public int ThreeStars { get; set; }
        public int TwoStars { get; set; }
        public int OneStar { get; set; }
    }

    public class SalesChartDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Sales { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }

    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }
}
