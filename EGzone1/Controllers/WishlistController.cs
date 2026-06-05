using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // لازم تكون مسجل دخول عشان تدير الـ Wishlist
    public class WishlistController : ControllerBase
    {
        private readonly MyDbContext _context;

        public WishlistController(MyDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // GET /api/Wishlist
        // جلب قائمة المفضلة للمستخدم الحالي
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> GetMyWishlist()
        {
            int userId = GetUserIdFromToken();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // 1. نجيب أو ننشئ الـ Wishlist الخاصة باليوزر ده
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p!.ProductImages)
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p!.ProductReviews)
                .FirstOrDefaultAsync(w => w.CustomerId == userId);

            if (wishlist == null || !wishlist.WishlistItems.Any())
            {
                return Ok(new { message = "قائمة المفضلة فارغة", data = new List<object>() });
            }

            // 2. تحويل البيانات لـ JSON نظيف
            var items = wishlist.WishlistItems.Select(wi => new
            {
                wishlistItemId = wi.WishlistItemId,
                productId = wi.Product!.ProductId,
                name = wi.Product.Name,
                price = wi.Product.Price,
                // أول صورة للمنتج
                imageUrl = wi.Product.ProductImages.Any()
                    ? baseUrl + wi.Product.ProductImages.First().ImageUrl
                    : wi.Product.ImageUrl,
                // متوسط التقييم
                rating = wi.Product.ProductReviews.Any()
                    ? Math.Round(wi.Product.ProductReviews.Average(r => r.Rating), 1)
                    : 0,
                inStock = (wi.Product.Stock ?? 0) > 0
            });

            return Ok(new { data = items });
        }

        // ====================================================================
        // POST /api/Wishlist/{productId}
        // إضافة منتج للمفضلة
        // ====================================================================
        [HttpPost("{productId:int}")]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            int userId = GetUserIdFromToken();

            // 1. التحقق من وجود المنتج
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!productExists)
                return NotFound(new { message = "المنتج غير موجود" });

            // 2. جلب أو إنشاء Wishlist للعميل
            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.CustomerId == userId);

            if (wishlist == null)
            {
                // إنشاء Customer record لو مش موجود (نفس منطق الـ Orders)
                var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == userId);
                if (!customerExists)
                {
                    _context.Customers.Add(new Customer { CustomerId = userId });
                    await _context.SaveChangesAsync();
                }

                wishlist = new Wishlist { CustomerId = userId };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            // 3. التحقق إن المنتج مش موجود في الـ Wishlist أصلاً (Unique Constraint)
            var alreadyExists = await _context.WishlistItems
                .AnyAsync(wi => wi.WishlistId == wishlist.WishlistId && wi.ProductId == productId);

            if (alreadyExists)
                return BadRequest(new { message = "المنتج موجود بالفعل في قائمة المفضلة" });

            // 4. إضافة العنصر
            var newItem = new WishlistItem
            {
                WishlistId = wishlist.WishlistId,
                ProductId = productId
            };

            _context.WishlistItems.Add(newItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تمت إضافة المنتج للمفضلة", wishlistItemId = newItem.WishlistItemId });
        }

        // ====================================================================
        // DELETE /api/Wishlist/{wishlistItemId}
        // إزالة منتج من المفضلة
        // ====================================================================
        [HttpDelete("{wishlistItemId:int}")]
        public async Task<IActionResult> RemoveFromWishlist(int wishlistItemId)
        {
            int userId = GetUserIdFromToken();

            // نتأكد إن الـ WishlistItem ده فعلاً بتاع اليوزر ده (Security Check)
            var item = await _context.WishlistItems
                .Include(wi => wi.Wishlist)
                .FirstOrDefaultAsync(wi => wi.WishlistItemId == wishlistItemId
                                        && wi.Wishlist!.CustomerId == userId);

            if (item == null)
                return NotFound(new { message = "العنصر غير موجود في قائمة المفضلة" });

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تمت إزالة المنتج من المفضلة" });
        }

        // ====================================================================
        // DELETE /api/Wishlist/clear
        // مسح كل المفضلة
        // ====================================================================
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearWishlist()
        {
            int userId = GetUserIdFromToken();

            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.CustomerId == userId);

            if (wishlist == null || !wishlist.WishlistItems.Any())
                return Ok(new { message = "قائمة المفضلة فارغة بالفعل" });

            _context.WishlistItems.RemoveRange(wishlist.WishlistItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم مسح قائمة المفضلة بالكامل" });
        }

        // ====================================================================
        // Helper
        // ====================================================================
        private int GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }
    }
}
