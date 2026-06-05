using Domain.Entities.Models;
using EGZone.DTOs;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 👈 مهم جداً عشان محدش يدخل من غير Token
    public class NotificationsController : ControllerBase
    {
        private readonly MyDbContext _context;

        public NotificationsController(MyDbContext context)
        {
            _context = context;
        }

        // 1. GET /api/Notifications (جلب كل الإشعارات لليوزر ده بس)
        [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            int userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized(new { message = "غير مصرح لك بالوصول" });

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt) // الأحدث يظهر الأول
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToListAsync();

            return Ok(notifications);
        }

        // 2. PUT /api/Notifications/:id/read (تغيير حالة الإشعار لـ "تمت القراءة")
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized(new { message = "غير مصرح لك بالوصول" });

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new { message = "الإشعار غير موجود" });
            }

            notification.IsRead = true; // غيرنا الحالة
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديد الإشعار كمقروء" });
        }

        // 3. PUT /api/Notifications/read-all (تغيير حالة كل الإشعارات لـ "تمت القراءة")
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized(new { message = "غير مصرح لك بالوصول" });

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
            {
                return Ok(new { message = "لا توجد إشعارات غير مقروءة" });
            }

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديد جميع الإشعارات كمقروءة" });
        }

        // 4. DELETE /api/Notifications/:id (حذف الإشعار)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            int userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized(new { message = "غير مصرح لك بالوصول" });

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new { message = "الإشعار غير موجود" });
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف الإشعار بنجاح" });
        }

        // Helper Method: لاستخراج الـ ID من التوكن بشكل آمن
        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return -1;
        }
    }
}