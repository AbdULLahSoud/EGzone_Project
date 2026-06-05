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
    public class CategoriesController : ControllerBase
    {
        private readonly MyDbContext _context;

        public CategoriesController(MyDbContext context)
        {
            _context = context;
        }

        // 1. عرض كل الأقسام الرئيسية مع الأقسام الفرعية
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Select(c => new
                {
                    id = c.CategoryId,
                    name = c.Name,
                    subCategories = c.SubCategories.Select(sc => new
                    {
                        id = sc.SubCategoryId,
                        name = sc.Name
                    })
                })
                .ToListAsync();

            return Ok(categories);
        }

        // 2. عرض قسم واحد بالتفصيل
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.CategoryId == id)
                .Select(c => new
                {
                    id = c.CategoryId,
                    name = c.Name,
                    subCategories = c.SubCategories.Select(sc => new
                    {
                        id = sc.SubCategoryId,
                        name = sc.Name
                    })
                })
                .FirstOrDefaultAsync();

            if (category == null) return NotFound(new { message = "القسم غير موجود" });

            return Ok(category);
        }

        // 3. إضافة قسم رئيسي جديد (Admin فقط)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            // التحقق من عدم تكرار الاسم
            var exists = await _context.Categories.AnyAsync(c => c.Name == dto.Name);
            if (exists)
                return BadRequest(new { message = "يوجد قسم بنفس الاسم بالفعل" });

            var category = new Category
            {
                Name = dto.Name
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء القسم بنجاح ✅", categoryId = category.CategoryId });
        }

        // 4. تعديل قسم رئيسي (Admin فقط)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(new { message = "القسم غير موجود" });

            // التحقق من عدم تكرار الاسم مع قسم آخر
            var exists = await _context.Categories.AnyAsync(c => c.Name == dto.Name && c.CategoryId != id);
            if (exists)
                return BadRequest(new { message = "يوجد قسم آخر بنفس الاسم" });

            category.Name = dto.Name;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تعديل القسم بنجاح ✅" });
        }

        // 5. حذف قسم رئيسي (Admin فقط)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null) return NotFound(new { message = "القسم غير موجود" });

            // منع الحذف لو فيه أقسام فرعية تابعة
            if (category.SubCategories.Any())
            {
                return BadRequest(new { message = "لا يمكن حذف القسم لأنه يحتوي على أقسام فرعية. احذف الأقسام الفرعية أولاً." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف القسم بنجاح ✅" });
        }
    }
}
