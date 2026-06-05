using Domain.Entities.Models;
using EGzone1.DTOs;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly MyDbContext _context; // استخدام اسم الـ DbContext الصحيح الخاص بك
        private readonly IWebHostEnvironment _environment;

        public BrandsController(MyDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("CreateBrand")]
        [Authorize] // حماية الـ Endpoint
        public async Task<IActionResult> CreateBrand([FromForm] CreateBrandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "خطأ: اسم البراند لا يمكن أن يكون فارغاً" });
            }

            // 1. تنظيف الاسم من الفراغات الزائدة
            string brandNameTrimmed = dto.Name.Trim();

            // 2. التحقق من وجود البراند مسبقاً لمنع التكرار
            var existingBrand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Name.ToLower() == brandNameTrimmed.ToLower());

            if (existingBrand != null)
            {
                // إذا كان موجوداً، نرجع الـ ID الخاص به فوراً للـ Flutter ليستعمله مباشرة
                return Ok(new
                {
                    message = "البراند موجود بالفعل في قاعدة البيانات",
                    brandId = existingBrand.BrandId,
                    name = existingBrand.Name
                });
            }

            // 3. إنشاء كائن البراند الجديد (بناءً على الأعمدة الموجودة في الـ Context لديك)
            var newBrand = new Brand
            {
                Name = brandNameTrimmed
                // ملاحظة: جدول الـ Brands لديك يحتوي فقط على (BrandId, Name) وفقاً للـ Context، لذا لا حاجة لحقول إضافية هنا.
            };

            // 4. حفظ البراند الجديد في قاعدة البيانات
            _context.Brands.Add(newBrand);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إضافة البراند الجديد بنجاح",
                brandId = newBrand.BrandId,
                name = newBrand.Name
            });
        }

        // ==========================================
        // 1. GET: عرض كل البراندات
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            var brands = await _context.Brands
                .Select(b => new { b.BrandId, b.Name })
                .ToListAsync();

            return Ok(brands);
        }

        // ==========================================
        // 2. GET: عرض براند واحد بالـ ID
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandById(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            if (brand == null)
                return NotFound(new { message = "البراند غير موجود!" });

            return Ok(new { brandId = brand.BrandId, name = brand.Name });
        }

        // ==========================================
        // 3. PUT: تعديل اسم البراند
        // ==========================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // يفضل يكون الأدمن بس اللي يعدل
        public async Task<IActionResult> UpdateBrand(int id, [FromForm] CreateBrandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "خطأ: اسم البراند لا يمكن أن يكون فارغاً" });

            // ندور على البراند في الداتا بيز
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound(new { message = "البراند غير موجود لتعديله!" });

            string newBrandNameTrimmed = dto.Name.Trim();

            // التأكد إن الاسم الجديد مش متكرر مع براند *تاني* غير اللي بنعدله
            bool isDuplicate = await _context.Brands.AnyAsync(b =>
                b.Name.ToLower() == newBrandNameTrimmed.ToLower() && b.BrandId != id);

            if (isDuplicate)
                return BadRequest(new { message = "يوجد براند آخر مسجل بنفس هذا الاسم!" });

            // التعديل والحفظ
            brand.Name = newBrandNameTrimmed;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم تعديل البراند بنجاح",
                brandId = brand.BrandId,
                name = brand.Name
            });
        }

        // ==========================================
        // 4. DELETE: حذف البراند
        // ==========================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // يفضل يكون الأدمن بس اللي يحذف
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            if (brand == null)
                return NotFound(new { message = "البراند غير موجود أصلاً!" });

            try
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف البراند بنجاح" });
            }
            catch (DbUpdateException)
            {
                // دي هتشتغل أوتوماتيك لو البراند مربوط بمنتجات والداتا بيز رفضت الحذف
                return BadRequest(new { message = "لا يمكن حذف هذا البراند لوجود منتجات مرتبطة به. الرجاء حذف المنتجات أولاً أو تغيير البراند الخاص بها." });
            }
        }





    }
}