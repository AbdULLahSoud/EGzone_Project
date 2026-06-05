using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class Size
{
    public int SizeId { get; set; }

    public string SizeValue { get; set; } = null!;

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
