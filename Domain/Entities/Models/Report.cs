using Infrastructure.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        // نوع المحتوى المُبلّغ عنه (Product, Review, User)
        [Required]
        [MaxLength(50)]
        public string ContentType { get; set; } = string.Empty;

        // ID المحتوى المُبلغ عنه
        public int ContentId { get; set; }

        // سبب الإبلاغ
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        // مين اللي بلّغ
        public int ReportedByUserId { get; set; }

        [ForeignKey("ReportedByUserId")]
        public virtual User ReportedByUser { get; set; } = null!;

        // الحالة (Pending, Resolved, Dismissed)
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
    }
}
