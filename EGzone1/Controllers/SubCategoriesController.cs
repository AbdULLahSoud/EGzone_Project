
using EGzone1.DTOs;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategoriesController : ControllerBase
    {
        private readonly MyDbContext _context;

        public SubCategoriesController(MyDbContext context)
        {
            _context = context;
        }

        // الـ API الأساسي اللي بيرجع الأقسام من الداتا بيز
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var subCategories = await _context.SubCategories
                .Select(sc => new SubCategoryDto
                {
                    Id = sc.SubCategoryId,
                    Name = sc.Name
                })
                .ToListAsync();

            return Ok(subCategories);
        }
    }
}