using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models;

public partial class ProductImage
{
    [Key]
    public int ImageId { get; set; }

    // جعلنا الـ ProductId إلزامي لأن الصورة لازم تتبع منتج
    public int ProductId { get; set; }

    // القيمة الافتراضية null! عشان نشيل الـ Warning
    public string ImageUrl { get; set; } = null!;

    // وصف الصورة اللي هيظهر في الـ JSON كـ semanticLabel
    public string? SemanticLabel { get; set; }

    public bool IsMain { get; set; } = false;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}