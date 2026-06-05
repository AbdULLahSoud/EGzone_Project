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
    [Authorize] // 👈 مفيش دفع من غير تسجيل دخول
    public class PaymentsController : ControllerBase
    {
        private readonly MyDbContext _context;

        public PaymentsController(MyDbContext context)
        {
            _context = context;
        }

        // 1. معالجة الدفع لطلب معين
        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPayment(PaymentRequestDto dto)
        {
            // 1. نجيب الـ ID بتاع اليوزر من التوكن
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdString);

            // 2. نجيب الطلب ونتأكد إنه موجود وبتاع العميل ده فعلاً
            var order = await _context.Orders
                
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId && o.CustomerId == userId);

            if (order == null)
                return NotFound("الطلب غير موجود أو لا تملك صلاحية الوصول إليه.");

            // 3. نتأكد إن الطلب مش مدفوع قبل كدة
            var existingPayment = await _context.Payments
                .AnyAsync(p => p.OrderId == dto.OrderId && p.PaymentStatus == "Paid");

            if (existingPayment)
                return BadRequest("تم دفع هذا الطلب مسبقاً!");

            // 4. محاكاة بوابة الدفع (بناءً على رقم الكارت الوهمي)
            bool paymentSuccess = SimulatePaymentGateway(dto.CardNumber);
            if (!paymentSuccess)
                return BadRequest("فشلت عملية الدفع. يرجى التأكد من رصيد البطاقة.");

            // 5. تسجيل عملية الدفع في الداتا بيز
            var payment = new Payment
            {
                OrderId = order.OrderId,
                MethodId = dto.MethodId, // مثلاً: 1 للفيزا، 2 لفوري
                PaymentMethod = "Credit Card",
                PaymentStatus = "Paid",
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // 6. تحديث حالة الطلب أوتوماتيك لـ "Processing" أو "تم الدفع"
            order.Status = "Processing";

            // 🌟 إضافة إشعار بنجاح الدفع
            var notification = new Domain.Entities.Models.Notification
            {
                UserId = userId,
                Title = "نجاح عملية الدفع",
                Message = $"تم تأكيد عملية الدفع للطلب رقم #{order.OrderId} بنجاح."
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم الدفع بنجاح!",
                paymentId = payment.PaymentId,
                orderStatus = order.Status
            });
        }

        // 2. عرض مدفوعاتي (عشان العميل يشوف فواتيره)
        [HttpGet("my-payments")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetMyPayments()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdString);

            var payments = await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.Order.CustomerId == userId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            return Ok(payments);
        }

        // ==========================================
        // Helpers (دوال مساعدة)
        // ==========================================

        // دالة وهمية لمحاكاة استجابة البنك (لو الكارت بيبدأ بـ 4 أو 5 ينجح)
        private bool SimulatePaymentGateway(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return false;

            // فيزا أو ماستركارد
            return cardNumber.StartsWith("4") || cardNumber.StartsWith("5");
        }
    }

    // الـ DTO اللي هيتبعت من Postman
    public class PaymentRequestDto
    {
        public int OrderId { get; set; }
        public int MethodId { get; set; } // رقم طريقة الدفع من جدول PaymentMethods
        public string CardNumber { get; set; } // للمحاكاة فقط
    }
}