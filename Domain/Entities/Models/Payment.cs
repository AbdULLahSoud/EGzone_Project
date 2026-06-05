using System;
using System.Collections.Generic;

namespace Infrastructure.Data.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? OrderId { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaidAt { get; set; }

    public int? MethodId { get; set; }

    public virtual PaymentMethod? Method { get; set; }

    public virtual Order? Order { get; set; }
}
