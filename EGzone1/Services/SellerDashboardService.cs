using EGzone1.Dto.Dashboard;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EGzone1.Services
{
    public class SellerDashboardService : ISellerDashboardService
    {
        private readonly MyDbContext _context;

        public SellerDashboardService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardResponseDto> GetDashboardDataAsync(int sellerId, string period)
        {
            var response = new DashboardResponseDto();
            var now = DateTime.UtcNow;

            // 1. Base query for seller's order items
            var sellerOrdersQuery = _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Product != null && oi.Product.SellerId == sellerId && oi.Order != null);

            var validOrdersQuery = sellerOrdersQuery.Where(oi => oi.Order!.Status != "Cancelled");

            // --- OVERVIEW & ORDER STATS ---
            var orderStatuses = await sellerOrdersQuery
                .GroupBy(oi => oi.Order!.Status)
                .Select(g => new { Status = g.Key ?? "Pending", Count = g.Select(oi => oi.OrderId).Distinct().Count() })
                .ToListAsync();

            response.OrderStats = new OrderStatsDto
            {
                Pending = orderStatuses.FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0,
                Processing = orderStatuses.FirstOrDefault(x => x.Status == "Processing")?.Count ?? 0,
                Shipped = orderStatuses.FirstOrDefault(x => x.Status == "Shipped")?.Count ?? 0,
                Delivered = orderStatuses.FirstOrDefault(x => x.Status == "Delivered")?.Count ?? 0,
                Cancelled = orderStatuses.FirstOrDefault(x => x.Status == "Cancelled")?.Count ?? 0
            };

            response.Overview = new OverviewDto
            {
                TotalRevenue = await validOrdersQuery.SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0)),
                TotalOrders = await sellerOrdersQuery.Select(oi => oi.OrderId).Distinct().CountAsync(),
                TotalCustomers = await sellerOrdersQuery.Select(oi => oi.Order!.CustomerId).Distinct().CountAsync(),
                PendingOrders = response.OrderStats.Pending,
                CompletedOrders = response.OrderStats.Delivered, 
                CancelledOrders = response.OrderStats.Cancelled
            };

            // --- PRODUCT STATS & LOW STOCK ---
            var productsQuery = _context.Products.AsNoTracking().Where(p => p.SellerId == sellerId && p.IsDeleted != true);
            
            response.Overview.TotalProducts = await productsQuery.CountAsync();
            response.ProductStats = new ProductStatsDto
            {
                ActiveProducts = await productsQuery.CountAsync(p => (p.Stock ?? 0) > 0),
                OutOfStock = await productsQuery.CountAsync(p => (p.Stock ?? 0) == 0),
                LowStock = await productsQuery.CountAsync(p => (p.Stock ?? 0) > 0 && (p.Stock ?? 0) <= 5)
            };

            response.LowStockProducts = await productsQuery
                .Where(p => (p.Stock ?? 0) <= 5)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name ?? "Unknown",
                    StockQuantity = p.Stock ?? 0
                })
                .ToListAsync();

            // --- REVENUE STATS ---
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var yearStart = new DateTime(now.Year, 1, 1);

            response.RevenueStats = new RevenueStatsDto
            {
                Today = await validOrdersQuery.Where(oi => oi.Order!.CreatedAt >= todayStart).SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0)),
                ThisWeek = await validOrdersQuery.Where(oi => oi.Order!.CreatedAt >= weekStart).SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0)),
                ThisMonth = await validOrdersQuery.Where(oi => oi.Order!.CreatedAt >= monthStart).SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0)),
                ThisYear = await validOrdersQuery.Where(oi => oi.Order!.CreatedAt >= yearStart).SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0))
            };

            // --- RECENT ORDERS ---
            response.RecentOrders = await sellerOrdersQuery
                .GroupBy(oi => new { oi.OrderId, oi.Order!.Customer!.CustomerNavigation.FullName, oi.Order.Status, oi.Order.CreatedAt })
                .OrderByDescending(g => g.Key.CreatedAt)
                .Take(10)
                .Select(g => new RecentOrderDto
                {
                    OrderId = g.Key.OrderId ?? 0,
                    CustomerName = g.Key.FullName ?? "Unknown",
                    TotalAmount = g.Sum(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0)),
                    Status = g.Key.Status ?? "Pending",
                    CreatedAt = g.Key.CreatedAt
                })
                .ToListAsync();

            // --- TOP PRODUCTS ---
            response.TopProducts = await validOrdersQuery
                .GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId ?? 0,
                    ProductName = g.Key.Name ?? "Unknown",
                    SoldQuantity = g.Sum(oi => oi.Quantity ?? 0),
                    Revenue = g.Sum(oi => (oi.Price ?? 0) * (oi.Quantity ?? 0))
                })
                .OrderByDescending(x => x.SoldQuantity)
                .Take(5)
                .ToListAsync();

            // --- REVIEW STATS ---
            var reviews = await _context.ProductReviews
                .AsNoTracking()
                .Where(pr => pr.Product != null && pr.Product.SellerId == sellerId)
                .Select(pr => pr.Rating)
                .ToListAsync();

            if (reviews.Any())
            {
                response.ReviewStats = new ReviewStatsDto
                {
                    TotalReviews = reviews.Count,
                    AverageRating = Math.Round(reviews.Average(), 1),
                    FiveStars = reviews.Count(r => r == 5),
                    FourStars = reviews.Count(r => r == 4),
                    ThreeStars = reviews.Count(r => r == 3),
                    TwoStars = reviews.Count(r => r == 2),
                    OneStar = reviews.Count(r => r == 1)
                };
            }

            // --- SALES CHART ---
            DateTime chartStartDate = period.ToLower() switch
            {
                "week" => now.AddDays(-7),
                "month" => now.AddDays(-30),
                "year" => now.AddYears(-1),
                _ => now.AddDays(-30) // Default to month
            };

            var rawChartData = await validOrdersQuery
                .Where(oi => oi.Order!.CreatedAt >= chartStartDate)
                .Select(oi => new { oi.Order!.CreatedAt, Revenue = (oi.Price ?? 0) * (oi.Quantity ?? 0) })
                .ToListAsync();

            if (period.ToLower() == "year")
            {
                response.SalesChart = rawChartData
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM"))
                    .Select(g => new SalesChartDto { Date = g.Key ?? "", Sales = g.Sum(x => x.Revenue) })
                    .OrderBy(x => x.Date)
                    .ToList();
            }
            else
            {
                response.SalesChart = rawChartData
                    .GroupBy(x => x.CreatedAt?.ToString("yyyy-MM-dd"))
                    .Select(g => new SalesChartDto { Date = g.Key ?? "", Sales = g.Sum(x => x.Revenue) })
                    .OrderBy(x => x.Date)
                    .ToList();
            }

            return response;
        }
    }
}
