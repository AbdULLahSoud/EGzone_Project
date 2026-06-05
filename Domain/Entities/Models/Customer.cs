using Domain.Entities.Models;
using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual User CustomerNavigation { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    
}
