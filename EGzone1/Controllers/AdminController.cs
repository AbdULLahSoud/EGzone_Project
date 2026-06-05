using Domain.Entities.Models;
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
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly MyDbContext _context;

        public AdminController(MyDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 📊 1. إحصائيات لوحة التحكم (Dashboard Stats)
        // يطابق: الكروت الثلاثة في أعلى الداش بورد
        // ============================================================
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalUsers = await _context.Users.CountAsync();

            // المنتجات اللي IsApproved = false ومش محذوفة
            var pendingApprovals = await _context.Products
                .IgnoreQueryFilters()
                .CountAsync(p => p.IsApproved == false && (p.IsDeleted == false || p.IsDeleted == null));

            var reportedContent = await _context.Reports
                .CountAsync(r => r.Status == "Pending");

            return Ok(new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                PendingApprovals = pendingApprovals,
                ReportedContent = reportedContent
            });
        }

        // ============================================================
        // 👥 2. إدارة المستخدمين (User Management)
        // يطابق: User Management → Ban or promote user accounts
        // ============================================================

        // 2.1 عرض كل المستخدمين
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserListDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        // 2.2 حظر/تنشيط مستخدم (Ban/Unban)
        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> ToggleBanUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

            // عكس الحالة الحالية
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = user.IsActive ? "تم تنشيط الحساب" : "تم حظر الحساب";
            return Ok(new { message = status, isActive = user.IsActive });
        }

        // 2.3 تغيير دور المستخدم (Promote/Demote)
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            // التحقق من صلاحية الدور المطلوب
            var validRoles = new[] { "Customer", "Seller", "Admin" };
            if (!validRoles.Contains(dto.NewRole))
            {
                return BadRequest(new { message = "الدور غير صالح. الأدوار المتاحة: Customer, Seller, Admin" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "المستخدم غير موجود" });

            var oldRole = user.Role;
            user.Role = dto.NewRole;
            user.UpdatedAt = DateTime.Now;

            // لو تم ترقيته لـ Seller ومفيش سجل Seller، نعمله واحد
            if (dto.NewRole == "Seller")
            {
                var existingSeller = await _context.Sellers.AnyAsync(s => s.UserId == id);
                if (!existingSeller)
                {
                    _context.Sellers.Add(new Seller
                    {
                        UserId = id,
                        StoreName = user.FullName + "'s Store",
                        Description = "متجر جديد",
                        ContactNumber = user.PhoneNumber ?? ""
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"تم تغيير الدور من {oldRole} إلى {dto.NewRole}" });
        }

        // ============================================================
        // 📦 3. مراقبة المنتجات (Product Moderation)
        // يطابق: Pending Approvals + Product Moderation
        // ============================================================

        // 3.1 عرض المنتجات المعلقة للموافقة
        [HttpGet("products/pending")]
        public async Task<IActionResult> GetPendingProducts()
        {
            var products = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => p.IsApproved == false && (p.IsDeleted == false || p.IsDeleted == null))
                .Include(p => p.Seller)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc!.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PendingProductDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    SellerName = p.Seller != null ? p.Seller.StoreName : "غير معروف",
                    CategoryName = p.SubCategory != null && p.SubCategory.Category != null
                        ? p.SubCategory.Category.Name : "بدون قسم",
                    SubCategoryName = p.SubCategory != null ? p.SubCategory.Name : "بدون قسم فرعي",
                    ImageUrl = p.ProductImages.Any()
                        ? p.ProductImages.First().ImageUrl : null,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        // 3.2 الموافقة على منتج
        [HttpPut("products/{id}/approve")]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { message = "المنتج غير موجود" });

            if (product.IsApproved)
                return BadRequest(new { message = "المنتج معتمد بالفعل" });

            product.IsApproved = true;
            product.UpdatedAt = DateTime.Now;

            // 🌟 إضافة إشعار للتاجر
            if (product.Seller != null)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = product.Seller.UserId,
                    Title = "تمت الموافقة على منتجك",
                    Message = $"تمت الموافقة على نشر منتجك '{product.Name}' وهو الآن متاح للعملاء."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم اعتماد المنتج بنجاح ✅", productId = id });
        }

        // 3.3 رفض منتج (Soft Delete)
        [HttpPut("products/{id}/reject")]
        public async Task<IActionResult> RejectProduct(int id)
        {
            var product = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { message = "المنتج غير موجود" });

            product.IsDeleted = true;
            product.IsApproved = false;
            product.UpdatedAt = DateTime.Now;

            // 🌟 إضافة إشعار للتاجر
            if (product.Seller != null)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = product.Seller.UserId,
                    Title = "تم رفض منتجك",
                    Message = $"نأسف، تم رفض نشر منتجك '{product.Name}' لمخالفته الشروط أو لوجود مشكلة به."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم رفض المنتج وحذفه ❌", productId = id });
        }

        // ============================================================
        // 🚨 4. إدارة البلاغات (Reported Content)
        // يطابق: Reported Content
        // ============================================================

        // 4.1 عرض كل البلاغات
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] string? status)
        {
            var query = _context.Reports
                .Include(r => r.ReportedByUser)
                .AsQueryable();

            // فلترة حسب الحالة لو تم تمريرها
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReportListDto
                {
                    ReportId = r.ReportId,
                    ContentType = r.ContentType,
                    ContentId = r.ContentId,
                    Reason = r.Reason,
                    ReportedByUserName = r.ReportedByUser.FullName,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ResolvedAt = r.ResolvedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        // 4.2 حل البلاغ (Resolve)
        [HttpPut("reports/{id}/resolve")]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound(new { message = "البلاغ غير موجود" });

            report.Status = "Resolved";
            report.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حل البلاغ بنجاح ✅" });
        }

        // 4.3 رفض البلاغ (Dismiss)
        [HttpPut("reports/{id}/dismiss")]
        public async Task<IActionResult> DismissReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound(new { message = "البلاغ غير موجود" });

            report.Status = "Dismissed";
            report.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم رفض البلاغ" });
        }

        // ============================================================
        // 📝 5. إنشاء بلاغ (متاح لأي مستخدم مسجل دخول)
        // ============================================================
        [AllowAnonymous]
        [HttpPost("reports")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(new { message = "يجب تسجيل الدخول أولاً" });

            var userId = int.Parse(userIdClaim.Value);

            // التحقق من نوع المحتوى
            var validTypes = new[] { "Product", "Review", "User" };
            if (!validTypes.Contains(dto.ContentType))
            {
                return BadRequest(new { message = "نوع المحتوى غير صالح. الأنواع المتاحة: Product, Review, User" });
            }

            var report = new Report
            {
                ContentType = dto.ContentType,
                ContentId = dto.ContentId,
                Reason = dto.Reason,
                ReportedByUserId = userId,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);

            // 🌟 إضافة إشعار للأدمن بوجود بلاغ جديد
            var admins = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).ToListAsync();
            foreach (var adminId in admins)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = adminId,
                    Title = "بلاغ جديد",
                    Message = $"تم إرسال بلاغ جديد بخصوص محتوى من نوع '{dto.ContentType}'. يرجى المراجعة."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إرسال البلاغ بنجاح، شكراً لك 🙏", reportId = report.ReportId });
        }

        // ============================================================
        // 🏪 6. إدارة طلبات التسجيل كتاجر (Seller Applications)
        // ============================================================

        // 6.1 عرض طلبات التسجيل المعلقة
        [HttpGet("sellers/pending")]
        public async Task<IActionResult> GetPendingSellerApplications()
        {
            var pending = await _context.Sellers
                .Where(s => s.Status == "Pending")
                .Include(s => s.SellerNavigation)
                .OrderByDescending(s => s.AppliedAt)
                .Select(s => new
                {
                    s.SellerId,
                    s.StoreName,
                    s.Description,
                    s.ContactNumber,
                    s.Status,
                    s.AppliedAt,
                    Applicant = new
                    {
                        s.SellerNavigation.UserId,
                        s.SellerNavigation.FullName,
                        s.SellerNavigation.Email,
                        s.SellerNavigation.PhoneNumber,
                        s.SellerNavigation.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(pending);
        }

        // 6.2 الموافقة على طلب تاجر
        [HttpPut("sellers/{id}/approve")]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.SellerNavigation)
                .FirstOrDefaultAsync(s => s.SellerId == id);

            if (seller == null)
                return NotFound(new { message = "الطلب غير موجود" });

            if (seller.Status == "Approved")
                return BadRequest(new { message = "هذا الطلب تمت الموافقة عليه بالفعل" });

            // ✅ تحديث حالة الطلب
            seller.Status = "Approved";

            // ✅ ترقية المستخدم لدور Seller
            var user = seller.SellerNavigation;
            if (user != null)
            {
                user.Role = "Seller";

                // 🌟 إشعار للتاجر الجديد
                _context.Notifications.Add(new Domain.Entities.Models.Notification
                {
                    UserId = user.UserId,
                    Title = "تهانينا! تمت الموافقة على طلبك ✅",
                    Message = $"أهلاً {user.FullName}! تمت الموافقة على تسجيلك كتاجر في EGZone. يمكنك الآن تسجيل الدخول والبدء في إضافة منتجاتك."
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تمت الموافقة على الطلب بنجاح ✅ وتم ترقية المستخدم إلى تاجر.",
                sellerId = id,
                storeName = seller.StoreName
            });
        }

        // 6.3 رفض طلب تاجر
        [HttpPut("sellers/{id}/reject")]
        public async Task<IActionResult> RejectSeller(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.SellerNavigation)
                .FirstOrDefaultAsync(s => s.SellerId == id);

            if (seller == null)
                return NotFound(new { message = "الطلب غير موجود" });

            if (seller.Status == "Rejected")
                return BadRequest(new { message = "هذا الطلب تم رفضه بالفعل" });

            // ❌ رفض الطلب
            seller.Status = "Rejected";

            // تحديث دور المستخدم إلى Customer (في حال كان PendingSeller)
            var user = seller.SellerNavigation;
            if (user != null && (user.Role == "PendingSeller" || user.Role == "Seller"))
            {
                user.Role = "Customer";

                // 🌟 إشعار للمتقدم بالرفض
                _context.Notifications.Add(new Domain.Entities.Models.Notification
                {
                    UserId = user.UserId,
                    Title = "بخصوص طلب التسجيل كتاجر",
                    Message = $"نأسف {user.FullName}، تم رفض طلب تسجيلك كتاجر في EGZone في الوقت الحالي. يمكنك التواصل مع الدعم لمعرفة السبب أو إعادة التقديم لاحقاً."
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم رفض الطلب ❌",
                sellerId = id,
                storeName = seller.StoreName
            });
        }

        // 6.4 عرض كل طلبات التسجيل (بفلتر اختياري)
        [HttpGet("sellers/applications")]
        public async Task<IActionResult> GetAllSellerApplications([FromQuery] string? status)
        {
            var query = _context.Sellers
                .Include(s => s.SellerNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(s => s.Status == status);

            var applications = await query
                .OrderByDescending(s => s.AppliedAt)
                .Select(s => new
                {
                    s.SellerId,
                    s.StoreName,
                    s.Description,
                    s.ContactNumber,
                    s.Status,
                    s.AppliedAt,
                    Applicant = new
                    {
                        s.SellerNavigation.UserId,
                        s.SellerNavigation.FullName,
                        s.SellerNavigation.Email,
                        s.SellerNavigation.PhoneNumber
                    }
                })
                .ToListAsync();

            return Ok(applications);
        }
    }
}
