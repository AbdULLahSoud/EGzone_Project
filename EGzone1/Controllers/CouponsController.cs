using EGzone1.Dto;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly MyDbContext _context;

        public CouponsController(MyDbContext context)
        {
            _context = context;
        }

        // 1. عرض كل الكوبونات (للأدمن فقط عشان يشوف الإحصائيات)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Coupon>>> GetCoupons()
        {
            return await _context.Coupons.ToListAsync();
        }

        // 2. أهم دالة: التحقق من الكوبون (لليوزر وهو بيشتري)
        [Authorize] // أي يوزر مسجل دخول
        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
                return NotFound(new { message = "Unvalid Code" });

            // التحقق من تاريخ الانتهاء
            if (coupon.ExpiryDate < DateTime.Now)
                return BadRequest(new { message = "Code is Up to Date" });

            // التحقق من عدد مرات الاستخدام
            if (coupon.UsedCount >= coupon.MaxUsage)
                return BadRequest(new { message = "Maximum Usage of this code has reached !" });

            return Ok(new
            {
                message = "كود سليم!",
                discount = coupon.DiscountPercent, // بنرجع النسبة بس
                isPercentage = true // بنبعتها بـ true عشان الموبايل يطمن إنها نسبة
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
        {
            // التعديل الاحترافي: نتأكد إن الكود مش متكرر الأول
            if (await _context.Coupons.AnyAsync(c => c.Code == dto.Code))
            {
                return BadRequest(new { message = "هذا الكود موجود مسبقاً، برجاء اختيار كود آخر!" });
            }

            var newCoupon = new Coupon
            {
                Code = dto.Code,
                DiscountPercent = dto.DiscountPercent,
                ExpiryDate = dto.ExpiryDate,
                MaxUsage = dto.MaxUsage,
                IsPercentage = true,
                UsedCount = 0
            };

            _context.Coupons.Add(newCoupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء الكوبون بنجاح!" });
        }



        // 4. حذف كوبون (للأدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Code Deleted !" });
        }



    }
}