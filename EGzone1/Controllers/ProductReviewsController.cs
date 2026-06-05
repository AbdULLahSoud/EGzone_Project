using EGZone.DTOs;
using Infrastructure.Data;
using Domain.Entities.Models; // تأكد إن ده الـ Namespace بتاع الـ ProductReview
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductReviewsController : ControllerBase
    {
        private readonly MyDbContext _context;

        public ProductReviewsController(MyDbContext context)
        {
            _context = context;
        }

        // 1. إضافة تقييم جديد (لازم يكون عامل Login)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview(CreateReviewDto dto)
        {
            // استخراج الـ ID بتاع اليوزر من الـ Token (سميناه CustomerId في الموديل)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int customerId = int.Parse(userIdClaim.Value);

            // تأكد إن المنتج موجود أصلاً
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists) return NotFound("المنتج غير موجود");

            // تأكد إن التقييم بين 1 و 5
            if (dto.Rating < 1 || dto.Rating > 5) return BadRequest("التقييم لازم يكون من 1 لـ 5 نجوم");

            var review = new ProductReview
            {
                ProductId = dto.ProductId,
                UserId = customerId, // مستخدمين الاسم الجديد
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(review);

            // 🌟 إضافة إشعار للتاجر بالتقييم الجديد
            var product = await _context.Products.Include(p => p.Seller).FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);
            if (product != null && product.Seller != null)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = product.Seller.UserId,
                    Title = "تقييم جديد لمنتجك",
                    Message = $"حصل منتجك '{product.Name}' على تقييم جديد ({dto.Rating} نجوم)."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إضافة التقييم بنجاح" });
        }

        // 2. جلب كل تقييمات منتج معين (مش محتاج Login)
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User) // عشان نجيب اسم اليوزر اللي قيم
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewReturnDto
                {
                    ReviewId = r.ReviewId,
                    CustomerName = r.User.FullName, // بيقرأ من علاقة الـ Customer
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();

            return Ok(reviews);
        }
    }
}