using Domain.Entities.Models;
using EGZone.DTOs;
using Infrastructure.Data; // عدل الـ Namespace حسب مشروعك
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 👈 مهم جداً: محدش يقدر يضيف أو يشوف عنوان غير لو عامل Login
    public class AddressesController : ControllerBase
    {
        private readonly MyDbContext _context;

        public AddressesController(MyDbContext context)
        {
            _context = context;
        }

        // 1. GET /api/Addresses
        // بيجيب عناوين اليوزر اللي عامل Login بس
        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            // 1. سحب الـ ID (بعد ما صلحنا الـ Warning في الدالة بتاعته)
            int userId = GetUserIdFromToken();

            // 2. سحب العناوين
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => new
                {
                    a.AddressId,
                    a.Street,
                    a.City,
                    a.Country
                }).ToListAsync();

            // 3. حركة إضافية: لو مفيش عناوين، عرف اليوزر بدل ما يرجع مصفوفة فاضية
            if (!addresses.Any())
            {
                return Ok(new { message = "لا توجد عناوين مسجلة لهذا الحساب", data = addresses });
            }

            return Ok(addresses);
        }

        // 2. POST /api/Addresses
        [HttpPost]
        public async Task<IActionResult> AddAddress([FromBody] CreateAddressDto dto)
        {
            int userId = GetUserIdFromToken();

            var address = new Address
            {
                UserId = userId,
                Street = dto.Street,
                City = dto.City,
                Country = dto.Country
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تمت إضافة العنوان بنجاح",
                addressId = address.AddressId
            });
        }

        // 3. DELETE /api/Addresses/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            int userId = GetUserIdFromToken();

            // بندور على العنوان بشرط يكون بتاع اليوزر ده بالذات عشان الأمان
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound(new { message = "العنوان غير موجود أو لا تملك صلاحية حذفه" });
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف العنوان بنجاح" });
        }

        // Helper Method: فنكشن صغيرة بتجيب الـ ID من التوكن عشان منكررش الكود
        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new UnauthorizedAccessException();
            return int.Parse(userIdClaim.Value);
        }
    }
}