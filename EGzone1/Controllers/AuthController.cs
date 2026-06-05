using BCrypt.Net;
using EGZone.DTOs;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        public AuthController(MyDbContext context, IConfiguration config , IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // 1. تسجيل حساب جديد
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already used");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.Now,
                Role = "Customer",
                // بنستخدم BCrypt هنا
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 🌟 إضافة إشعار ترحيبي
            var notification = new Domain.Entities.Models.Notification
            {
                UserId = user.UserId,
                Title = "أهلاً بك في EGZone",
                Message = $"أهلاً بك يا {user.FullName} في متجرنا! نتمنى لك تجربة تسوق ممتعة."
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account Created Successfully" });
        }

        // 2. تسجيل الدخول
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized("الإيميل أو كلمة المرور غلط!");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                userName = user.FullName
            });
        }

        // 3. نسيان كلمة المرور (إرسال التوكن)
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) return Ok("Reset link sent if email exists.");

            var resetToken = Guid.NewGuid().ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiration = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            // ✅ دلوقتي الإرسال شغال بجد!
            var resetLink = $"http://localhost:3000/reset-password?token={resetToken}&email={user.Email}";
            await _emailService.SendEmailAsync(user.Email, "Reset Password - EGZone",
                $"<h1>Reset Your Password</h1><p>Click <a href='{resetLink}'>here</a> to reset.</p>");

            return Ok("Email sent successfully!");
        }

        // 4. تعيين كلمة المرور الجديدة
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

            if (user == null || user.ResetTokenExpiration < DateTime.UtcNow)
                return BadRequest("Invalid or expired token.");

            // ✅ التعديل هنا: استخدمنا BCrypt عشان يبقى زي الـ Register
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            user.ResetToken = null;
            user.ResetTokenExpiration = null;

            await _context.SaveChangesAsync();

            // 🌟 إضافة إشعار بإعادة تعيين كلمة المرور
            var notification = new Domain.Entities.Models.Notification
            {
                UserId = user.UserId,
                Title = "تغيير كلمة المرور",
                Message = "تم إعادة تعيين كلمة المرور الخاصة بك بنجاح."
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok("Password updated successfully.");
        }
    }
}