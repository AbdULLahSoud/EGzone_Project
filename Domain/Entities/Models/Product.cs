using Domain.Entities.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace Infrastructure.Data.Models;
    
public partial class Product
{
    public int ProductId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public int? Stock { get; set; }

    public int? SellerId { get; set; }

    public int? SubCategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    //public bool? IsDeleted { get; set; }
    public bool IsDeleted { get; set; } = false;

    // المنتجات الجديدة تدخل قائمة الانتظار للموافقة من الأدمن
    public bool IsApproved { get; set; } = false;

    public int? BrandId { get; set; }

    public virtual Brand? Brand { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

  
    public virtual ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();

    public virtual Seller? Seller { get; set; }

    public virtual SubCategory? SubCategory { get; set; }

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    
  

   

}
