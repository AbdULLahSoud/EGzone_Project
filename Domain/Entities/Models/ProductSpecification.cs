using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Data.Models;

public partial class ProductSpecification
{
    [Key]
    public int Id { get; set; }

    // اسم الخاصية مثل "الذاكرة" أو "المعالج"
    public string Label { get; set; } = null!;

    // قيمة الخاصية مثل "12GB" أو "Snapdragon 8 Gen 3"
    public string Value { get; set; } = null!;

    // الربط مع المنتج
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}