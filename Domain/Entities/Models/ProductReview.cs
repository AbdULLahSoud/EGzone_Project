using Infrastructure.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewId { get; set; }

        // ربط المنتج صراحة
        public int ProductId { get; set; }
        [ForeignKey("ProductId")] // 👈 دي اللي بتمنع ظهور ProductId1
        public virtual Product Product { get; set; }

        // ربط اليوزر صراحة (استخدمنا UserId عشان نطابق User.cs)
        public int UserId { get; set; }
        [ForeignKey("UserId")] // 👈 دي اللي بتمنع ظهور CustomerId1
        public virtual User User { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}