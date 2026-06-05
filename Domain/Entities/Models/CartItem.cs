using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int? CartId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    // ✅ تغيير من string لـ int عشان يتوافق مع UserId في جدول Users
    public int UserId { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Product? Product { get; set; }

}
