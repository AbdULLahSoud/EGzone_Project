using System;
using System.Collections.Generic;
using Domain.Entities.Models; // تأكد إن ده الـ Namespace اللي فيه كلاس ProductReview

namespace Infrastructure.Data.Models
{
    public partial class User
    {
        // 1. الأساسيات (Primary Key & Info)
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Customer"; // الأدوار: Customer, Vendor, Admin

        // 2. بيانات البروفايل
        public string? ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // 3. حقول الأمان والتحقق
        public bool IsEmailVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiration { get; set; }

        // 4. العلاقات (Navigation Properties) - One-to-One
        // ملاحظة: دي العلاقات اللي كانت مسببة الـ Conflict لما كان اسم العلاقة "Customer"
        public virtual Admin? Admin { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual Seller? Seller { get; set; }

        // 5. علاقة التقييمات (One-to-Many)
        // لازم نستخدم "ProductReviews" بالجمع عشان تمشي مع الـ DbContext اللي صلحناه
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}