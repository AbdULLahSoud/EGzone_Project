using BCrypt.Net;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EGzone1.Services;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellersController : ControllerBase
    {
        private readonly MyDbContext _context;

        public SellersController(MyDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 💡 دالة مساعدة (Helper) لتوفير تكرار الكود: بتجيب التاجر الحالي من التوكن
        // ==========================================
        private async Task<Seller> GetCurrentSellerAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return null;

            return await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        }

        // ==========================================
        // 1. عرض كل التجار (للأدمن فقط) - ✅ تم إضافة Pagination لتسريع الأداء
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Seller>>> GetSellers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var sellers = await _context.Sellers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(sellers);
        }

        // ==========================================
        // 2. عرض بروفايل التاجر الحالي
        // ==========================================
        [Authorize(Roles = "Seller,Admin")]
        [HttpGet("my-profile")]
        public async Task<ActionResult<Seller>> GetMyProfile()
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound(new { message = "إنت لسا معملتش بروفايل تاجر" });

            // سحب المنتجات المرتبطة بيه
            seller.Products = await _context.Products.Where(p => p.SellerId == seller.SellerId && p.IsDeleted != true).ToListAsync();

            return Ok(seller);
        }

        // ==========================================
        // 3. التسجيل كتاجر (للمستخدم المسجل بالفعل)
        // الحالة: Pending → بانتظار موافقة الأدمن
        // ==========================================
        [Authorize]
        [HttpPost("register-as-seller")]
        public async Task<ActionResult<Seller>> PostSeller([FromBody] SellerRequestDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdString);

            var alreadySeller = await _context.Sellers.AnyAsync(s => s.UserId == userId);
            if (alreadySeller) return BadRequest(new { message = "إنت عندك طلب تاجر بالفعل!" });

            var newSeller = new Seller
            {
                UserId = userId,
                StoreName = dto.StoreName,
                Description = dto.Description,
                ContactNumber = dto.ContactNumber,
                Status = "Pending",
                AppliedAt = DateTime.Now
            };

            _context.Sellers.Add(newSeller);

            // ❌ لا نغير الرول هنا — بينتظر موافقة الأدمن
            var user = await _context.Users.FindAsync(userId);

            // 🌟 إشعار للأدمن
            var admins = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).ToListAsync();
            foreach (var adminId in admins)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = adminId,
                    Title = "طلب تسجيل تاجر جديد",
                    Message = $"المستخدم '{(user != null ? user.FullName : "مستخدم")}' قدّم طلبًا للتسجيل كتاجر باسم متجر: '{dto.StoreName}'. يرجى المراجعة والموافقة أو الرفض."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إرسال طلبك بنجاح! سيتم مراجعته من قِبل الإدارة وستصلك إشعار بالنتيجة.", sellerId = newSeller.SellerId });
        }

        // ==========================================
        // 🆕 3b. التسجيل كتاجر بدون حساب مسبق
        // بيعمل حساب يوزر جديد وسيلر بحالة Pending في خطوة واحدة
        // ==========================================
        [AllowAnonymous]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyAsSeller([FromBody] SellerApplicationDto dto)
        {
            // 1. التحقق من الإيميل
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "هذا الإيميل مسجل بالفعل. سجّل دخولك واطلب التسجيل كتاجر." });

            // 2. إنشاء حساب المستخدم بدور PendingSeller مؤقت
            var newUser = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "PendingSeller",   // دور مؤقت لحين موافقة الأدمن
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // نحفظ عشان نجيب UserId

            // 3. إنشاء ملف السيلر بحالة Pending
            var newSeller = new Seller
            {
                UserId = newUser.UserId,
                StoreName = dto.StoreName,
                Description = dto.Description,
                ContactNumber = dto.PhoneNumber,
                Status = "Pending",
                AppliedAt = DateTime.Now
            };

            _context.Sellers.Add(newSeller);

            // 4. إشعار ترحيبي للمتقدم
            _context.Notifications.Add(new Domain.Entities.Models.Notification
            {
                UserId = newUser.UserId,
                Title = "تم استلام طلبك",
                Message = $"أهلاً {dto.FullName}! تم استلام طلب تسجيلك كتاجر بنجاح. سيتم مراجعته خلال 24-48 ساعة وستصلك إشعار بالنتيجة."
            });

            // 5. إشعار للأدمن
            var admins = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).ToListAsync();
            foreach (var adminId in admins)
            {
                _context.Notifications.Add(new Domain.Entities.Models.Notification
                {
                    UserId = adminId,
                    Title = "طلب تسجيل تاجر جديد يحتاج موافقتك",
                    Message = $"'{dto.FullName}' طلب التسجيل كتاجر باسم متجر: '{dto.StoreName}'. راجع الطلب وافعل رد مناسب."
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إرسال طلبك بنجاح! ✅ سيتم مراجعته من قِبل الإدارة وستصلك إشعار على إيميلك بالنتيجة.",
                userId = newUser.UserId,
                sellerId = newSeller.SellerId
            });
        }

        // ==========================================
        // 🆕 4. تعديل بيانات التاجر (تحديث البروفايل)
        // ==========================================
        [Authorize(Roles = "Seller")]
        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] SellerRequestDto dto)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound(new { message = "حساب التاجر غير موجود" });

            seller.StoreName = dto.StoreName;
            seller.Description = dto.Description;
            seller.ContactNumber = dto.ContactNumber;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تحديث بيانات المتجر بنجاح", data = seller });
        }

        // ==========================================
        // 5. حذف تاجر (للأدمن فقط)
        // ==========================================
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeller(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null) return NotFound(new { message = "التاجر غير موجود" });

            _context.Sellers.Remove(seller);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف التاجر بنجاح" });
        }

        // ==========================================
        // 🆕 6. إضافة منتج جديد للمتجر الخاص بالتاجر
        // ==========================================
        [Authorize(Roles = "Seller")]
        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] SellerProductDto dto)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return Unauthorized(new { message = "غير مصرح لك" });

            var newProduct = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                SubCategoryId = dto.SubCategoryId, // 👈 التعديل هنا لـ SubCategoryId
                BrandId = dto.BrandId,             // 👈 ضفنا البراند
                ImageUrl = dto.ImageUrl,           // 👈 ضفنا الصورة
                SellerId = seller.SellerId, // 👈 أهم سطر: ربط المنتج بالتاجر الحالي
                IsDeleted = false
            };

            _context.Products.Add(newProduct);

            // 🌟 إضافة إشعار للأدمن بوجود منتج جديد يحتاج مراجعة
            var adminsForProduct = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).ToListAsync();
            foreach (var adminId in adminsForProduct)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = adminId,
                    Title = "منتج جديد بانتظار الموافقة",
                    Message = $"قام التاجر '{seller.StoreName}' بإضافة منتج جديد '{dto.Name}' يحتاج إلى مراجعتك واعتمادك."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إضافة المنتج لمتجرك بنجاح!", productId = newProduct.ProductId });
        }

        // ==========================================
        // 🆕 7. تعديل منتج خاص بالتاجر
        // ==========================================
        [Authorize(Roles = "Seller")]
        [HttpPut("products/{productId}")]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] SellerProductDto dto)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return Unauthorized(new { message = "غير مصرح لك" });

            // 👈 لازم نتأكد إن المنتج موجود، وإن الـ SellerId بتاعه هو نفس الـ SellerId بتاع التاجر اللي بيعدل
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId && p.SellerId == seller.SellerId);

            if (product == null)
                return NotFound(new { message = "المنتج غير موجود أو لا تملك صلاحية تعديله!" });

            // تحديث البيانات
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.SubCategoryId = dto.SubCategoryId;
            product.BrandId = dto.BrandId;


            // لو بعت صورة جديدة نحدثها، لو مبعتش نسيب القديمة
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                product.ImageUrl = dto.ImageUrl;
            }


            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تعديل المنتج بنجاح!" });
        }

        // ==========================================
        // 8. داشبورد السيلر - ✅ تم حل مشكلة الـ Performance (العمليات الحسابية بقت في الداتا بيز)
        // ==========================================
        [Authorize(Roles = "Seller,Admin")]
        [HttpGet("dashboard/analytics")]
        public async Task<IActionResult> GetDashboardAnalytics()
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

            // تجهيز الكويري بدون تنفيذه (IQueryable)
            var sellerOrderItemsQuery = _context.OrderItems
                .Where(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId && oi.Order != null && oi.Order.Status != "Cancelled");

            // 1. حساب المبيعات (الجمع بيتم في الـ SQL)
            decimal totalRevenue = await sellerOrderItemsQuery.SumAsync(oi => (oi.Price ?? 0) * (oi.Quantity ?? 1));

            // 2. إجمالي الطلبات 
            int totalOrders = await sellerOrderItemsQuery.Select(oi => oi.OrderId).Distinct().CountAsync();

            // 3. المنتجات الفعالة والتي نفدت
            var productsQuery = _context.Products.Where(p => p.SellerId == seller.SellerId && p.IsDeleted != true);
            int activeProducts = await productsQuery.CountAsync();
            int outOfStockProducts = await productsQuery.CountAsync(p => (p.Stock ?? 0) == 0);

            // 4. أحدث 5 مبيعات
            var recentSales = await sellerOrderItemsQuery
                .OrderByDescending(oi => oi.Order!.CreatedAt)
                .Take(5)
                .Select(oi => new
                {
                    OrderId = oi.OrderId,
                    ProductName = oi.Product!.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    Date = oi.Order!.CreatedAt,
                    Status = oi.Order!.Status
                })
                .ToListAsync();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                ActiveProducts = activeProducts,
                OutOfStockProducts = outOfStockProducts,
                RecentSales = recentSales
            });
        }

        // ==========================================
        // 9. داشبورد السيلر الشاملة (Service)
        // ==========================================
        [Authorize(Roles = "Seller,Admin")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetSellerDashboard([FromServices] ISellerDashboardService dashboardService, [FromQuery] string period = "month")
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

            var dashboardData = await dashboardService.GetDashboardDataAsync(seller.SellerId, period);
            return Ok(dashboardData);
        }
    }

    // ==========================================
    // DTOs
    // ==========================================
    public class SellerRequestDto
    {
        public string StoreName { get; set; }
        public string Description { get; set; }
        public string ContactNumber { get; set; }
    }

    // DTO للتسجيل كتاجر بدون حساب مسبق
    public class SellerApplicationDto
    {
        // بيانات الحساب الجديد
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? PhoneNumber { get; set; }

        // بيانات المتجر
        public string StoreName { get; set; }
        public string Description { get; set; }
    }

    // الـ DTO الجديد بعد التعديل ليتطابق مع الـ Product Model
    public class SellerProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int? SubCategoryId { get; set; } // ✅ 
        public int? BrandId { get; set; }       // ✅ 
        public string? ImageUrl { get; set; }   // ✅ 
    }
}