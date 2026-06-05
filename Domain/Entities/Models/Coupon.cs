using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string? Code { get; set; }

    public int? DiscountPercent { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? MaxUsage { get; set; }

    public int? UsedCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } // قيمة الخصم
    public bool IsPercentage { get; set; }     // هل هو نسبة مئوية؟ (صح/خطأ)
   
    
    


    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
