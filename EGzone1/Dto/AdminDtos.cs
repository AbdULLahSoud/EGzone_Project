namespace EGzone1.Dto
{
    // ✅ إحصائيات لوحة التحكم الرئيسية (الكروت الثلاثة)
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int PendingApprovals { get; set; }
        public int ReportedContent { get; set; }
    }

    // ✅ بيانات المستخدم في قائمة إدارة المستخدمين
    public class UserListDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    // ✅ تغيير دور المستخدم (ترقية/تنزيل)
    public class UpdateUserRoleDto
    {
        public string NewRole { get; set; } = string.Empty; // Customer, Seller, Admin
    }

    // ✅ بيانات المنتج المعلق للمراجعة
    public class PendingProductDto
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? SellerName { get; set; }
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // ✅ إنشاء بلاغ جديد (للمستخدمين العاديين)
    public class CreateReportDto
    {
        public string ContentType { get; set; } = string.Empty; // Product, Review, User
        public int ContentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // ✅ عرض البلاغات للأدمن
    public class ReportListDto
    {
        public int ReportId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int ContentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ReportedByUserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    // ✅ إنشاء/تعديل قسم رئيسي
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
