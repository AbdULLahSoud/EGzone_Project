using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class OrderStatus
{
    public int StatusId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
