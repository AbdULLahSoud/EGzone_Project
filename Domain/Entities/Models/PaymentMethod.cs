using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class PaymentMethod
{
    public int MethodId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
