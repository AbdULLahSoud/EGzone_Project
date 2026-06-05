using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models;

public partial class Seller
{
    [Key] 
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SellerId { get; set; }

    public string? StoreName { get; set; }

    [ForeignKey("ApplicationUser")]
    public int UserId { get; set; }      
    public string Description { get; set; }   
    public string ContactNumber { get; set; }

    // حالة الطلب: Pending = بانتظار الموافقة, Approved = تمت الموافقة, Rejected = تم الرفض
    public string Status { get; set; } = "Pending";

    // تاريخ تقديم الطلب
    public DateTime AppliedAt { get; set; } = DateTime.Now;

    public List<Product> Products { get; set; } = new List<Product>();

    public virtual User SellerNavigation { get; set; } = null!;
}
