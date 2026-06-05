using EGzone1.Dto;
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
    [Authorize] // 🔒 ممنوع الدخول لغير المسجلين في السيستم كله
    public class CartItemsController : ControllerBase
    {
        private readonly MyDbContext _context;

        public CartItemsController(MyDbContext context)
        {
            _context = context;
        }

        // 1. عرض سلة "المستخدم الحالي" فقط (Privacy)
        [HttpGet]
        public async Task<ActionResult> GetMyCart()
        {
            // ✅ استخدام int بدل string
            int userId = GetUserIdFromToken();

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .Select(ci => new {
                    ci.CartItemId,
                    ci.ProductId,
                    ProductName = ci.Product.Name,
                    ImageUrl = ci.Product.ImageUrl,
                    Price = ci.Product.Price,
                    ci.Quantity,
                    TotalPrice = ci.Quantity * ci.Product.Price
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        // 2. إضافة منتج للسلة
        [HttpPost]
        public async Task<ActionResult> PostCartItem(CartItemRequestDto dto)
        {
            int userId = GetUserIdFromToken();

            if (dto.Quantity <= 0) return BadRequest("Quantity must be more than 0");

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists) return NotFound("Product Not Found");

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    UserId = userId, // ✅ int
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _context.CartItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Added to Cart!" });
        }

        // 3. حذف عنصر من السلة
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            int userId = GetUserIdFromToken();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.UserId == userId);

            if (cartItem == null)
                return NotFound("Item not Found in the Cart");

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item deleted" });
        }

        // ✅ Helper: استخراج UserId كـ int من الـ Token
        private int GetUserIdFromToken()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(claim.Value);
        }
    }
}