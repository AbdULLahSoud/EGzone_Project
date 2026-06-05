using EGZone.DTOs;
using EGzone1.Dto;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class UserProfileController : ControllerBase
    {
        private readonly MyDbContext _context;

        public UserProfileController(MyDbContext context)
        {
            _context = context;
        }

        // GET: api/UserProfile/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // بنجيب الـ ID من الـ Claims اللي جوه الـ Token
            var userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized();

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserProfileDto
                {
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound("User Not Found !");

            return Ok(user);
        }

        // PUT: api/UserProfile/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // تحديث الحقول
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Updated !", user.FullName });
            }
            catch (Exception ex)
            {
                return BadRequest("an error occured during updating data " + ex.Message);
            }
        }

        
        private int GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : -1;
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // 1. نجيب الـ ID من الـ Token
            var userId = GetUserIdFromToken();
            if (userId == -1) return Unauthorized();

            // 2. نجيب بيانات اليوزر من الداتا بيز
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("المستخدم غير موجود");

            // 3. نتأكد إن الباسورد "الحالي" اللي كتبه صح (مقارنة مع اللي في الـ DB)
            // بنستخدم BCrypt.Verify هنا
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("كلمة المرور الحالية غير صحيحة");
            }

            // 4. نشفر الباسورد "الجديد" ونحفظه
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            // 🌟 إضافة إشعار بتغيير كلمة المرور
            var notification = new Domain.Entities.Models.Notification
            {
                UserId = userId,
                Title = "تغيير كلمة المرور",
                Message = "تم تغيير كلمة المرور الخاصة بك بنجاح من إعدادات الحساب."
            };
            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (Exception ex)
            {
                return BadRequest("حدث خطأ أثناء حفظ كلمة المرور الجديدة");
            }
        }






    }
}