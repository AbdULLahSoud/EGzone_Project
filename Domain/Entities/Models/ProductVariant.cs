using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int? ProductId { get; set; }

    public int? ColorId { get; set; }

    public int? SizeId { get; set; }

    public int? Stock { get; set; }

    public decimal? PriceAdjustment { get; set; }

    public virtual Color? Color { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Size? Size { get; set; }
}
