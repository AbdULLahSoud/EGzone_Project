//using System.Security.Claims;
//using Infrastructure.Data;
//using Infrastructure.Data.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace EGzone1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [Authorize]
//    public class OrdersController : ControllerBase
//    {
//        private readonly MyDbContext _context;

//        public OrdersController(MyDbContext context)
//        {
//            _context = context;
//        }

//        // 1. الدالة الأساسية: إنشاء الطلب من السلة
//        [HttpPost("place-order")]
//        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
//        {
//            // 1. نجيب الـ ID بتاع اليوزر من التوكن 
//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            int customerId = int.Parse(userIdString);

//            // 🌟🌟 الحل السحري للإيرور 500: التأكد من وجود العميل في جدول العملاء 🌟🌟
//            // الداتا بيز كانت بترفض تسجل الأوردر عشان اليوزر ده ملوش بروفايل في جدول Customers
//            var existingCustomer = await _context.Customers.FindAsync(customerId);
//            if (existingCustomer == null)
//            {
//                // لو اليوزر ملوش سجل، هنكريتله واحد فوراً بنفس الـ ID عشان الداتا بيز تقبل
//                var newCustomerProfile = new Customer
//                {
//                    CustomerId = customerId
//                };
//                _context.Customers.Add(newCustomerProfile);
//                await _context.SaveChangesAsync();
//            }
//            // -------------------------------------------------------------

//            // 2. هنجيب المنتجات من CartItems مباشرة زي ما دالة الـ GET بتعمل
//            var cartItems = await _context.CartItems
//                .Include(ci => ci.Product)
//                .Where(ci => ci.UserId == customerId) // ✅ مقارنة int بـ int
//                .ToListAsync();

//            // 3. الفحوصات بتاعتنا
//            if (cartItems == null || !cartItems.Any())
//            {
//                return BadRequest(new { message = "سلتك فاضية يا فندم، مفيش حاجة نطلبها!" });
//            }

//            // 4. حساب الإجمالي المبدئي والتأكد من المخزون (مع حساب الكمية Quantity)
//            decimal totalAmount = 0;
//            foreach (var item in cartItems)
//            {
//                if (item.Product.Stock < item.Quantity)
//                    return BadRequest(new { message = $"المنتج '{item.Product.Name}' مفيش منه كمية تكفي في المخزن!" });

//                // بنضرب السعر في الكمية
//                totalAmount += (decimal)(item.Product.Price * item.Quantity);
//            }

//            // 5. معالجة الكوبون (لو العميل باعت كود)
//            Coupon appliedCoupon = null;
//            if (!string.IsNullOrEmpty(dto.CouponCode))
//            {
//                appliedCoupon = await _context.Coupons
//                    .FirstOrDefaultAsync(c => c.Code == dto.CouponCode);

//                if (appliedCoupon == null)
//                    return BadRequest(new { message = "كود الكوبون ده مش موجود!" });

//                if (appliedCoupon.ExpiryDate < DateTime.Now)
//                    return BadRequest(new { message = "الكوبون ده منتهي الصلاحية!" });

//                if (appliedCoupon.UsedCount >= appliedCoupon.MaxUsage)
//                    return BadRequest(new { message = "الكوبون ده وصل للحد الأقصى للاستخدام!" });

//                // ✅ تم التصحيح: بيشوف نوع الكوبون ويحسب صح
//                decimal discount = appliedCoupon.IsPercentage
//                    ? totalAmount * ((decimal)(appliedCoupon.DiscountPercent ?? 0) / 100)  // نسبة مئوية
//                    : appliedCoupon.DiscountAmount;                                         // مبلغ ثابت

//                totalAmount -= discount;
//                if (totalAmount < 0) totalAmount = 0; // ضمان التوتال ما يبقاش سالب
//            }

//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                // أ. إنشاء الطلب
//                var newOrder = new Order
//                {
//                    CustomerId = customerId,
//                    AddressId = dto.AddressId,
//                    TotalAmount = totalAmount,
//                    Status = "Pending",
//                    PaymentMethod = dto.PaymentMethod,
//                    CouponId = appliedCoupon?.CouponId,
//                    CreatedAt = DateTime.Now
//                };
//                _context.Orders.Add(newOrder);
//                await _context.SaveChangesAsync();

//                // ب. نقل المنتجات لجدول الـ OrderItems وتقليل المخزون
//                foreach (var item in cartItems)
//                {
//                    var orderItem = new OrderItem
//                    {
//                        OrderId = newOrder.OrderId,
//                        ProductId = (int)item.ProductId,
//                        Price = item.Product.Price,
//                        Quantity = item.Quantity
//                    };
//                    _context.OrderItems.Add(orderItem);

//                    // خصم الكمية اللي اشتراها من المخزن
//                    item.Product.Stock -= item.Quantity;
//                }

//                // ج. تفريغ السلة (مسح المنتجات الخاصة باليوزر ده)
//                _context.CartItems.RemoveRange(cartItems);

//                // د. تحديث عدد استخدامات الكوبون (لو تم استخدامه)
//                if (appliedCoupon != null)
//                {
//                    appliedCoupon.UsedCount += 1;
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                return Ok(new
//                {
//                    message = "تم تسجيل طلبك بنجاح!",
//                    orderId = newOrder.OrderId,
//                    totalToPay = totalAmount
//                });
//            }
//            catch (Exception ex)
//            {
//                // هنرجع في أي تغييرات حصلت
//                await transaction.RollbackAsync();

//                // هنجيب الإيرور الداخلي الحقيقي بتاع Entity Framework
//                string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

//                // هنطبع الإيرور عشان نعرف الداتا بيز رافضة إيه بالظبط
//                return StatusCode(500, new { message = "تفاصيل المشكلة: " + realError });
//            }
//        }

//        [HttpGet("my-orders")]
//        public async Task<ActionResult> GetMyOrders()
//        {
//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            int customerId = int.Parse(userIdString);

//            var orders = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .Where(o => o.CustomerId == customerId)
//                .OrderByDescending(o => o.CreatedAt)
//                .Select(o => new
//                {
//                    o.OrderId,
//                    o.TotalAmount,
//                    o.Status,
//                    o.PaymentMethod,
//                    o.CreatedAt,
//                    // بنجيب تفاصيل المنتجات جوه الأوردر بطريقة نضيفة
//                    Items = o.OrderItems.Select(oi => new
//                    {
//                        oi.ProductId,
//                        ProductName = oi.Product.Name,
//                        ImageUrl = oi.Product.ImageUrl, // عشان الصورة تظهر في الطلبات السابقة برضه
//                        oi.Price
//                        // Quantity = oi.Quantity // لو كنت ضفت الكمية في OrderItems
//                    })
//                })
//                .ToListAsync();

//            return Ok(orders);
//        }

//        [HttpGet("seller-orders")]
//        [Authorize(Roles = "Seller,Admin")]
//        public async Task<IActionResult> GetSellerOrders()
//        {
//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
//            {
//                return Unauthorized(new { message = "المستخدم غير معرف" });
//            }

//            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
//            if (seller == null) return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

//            // نجيب الأوردرات اللي فيها منتجات للتاجر ده
//            var orders = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .Include(o => o.Customer)
//                .ThenInclude(c => c!.CustomerNavigation)
//                .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId))
//                .OrderByDescending(o => o.CreatedAt)
//                .Select(o => new
//                {
//                    o.OrderId,
//                    o.TotalAmount,
//                    o.Status,
//                    o.PaymentMethod,
//                    o.CreatedAt,
//                    CustomerName = o.Customer != null && o.Customer.CustomerNavigation != null ? o.Customer.CustomerNavigation.FullName : "Unknown",
//                    o.CustomerId,
//                    // نعرض للتاجر فقط المنتجات الخاصة به في هذا الأوردر
//                    Items = o.OrderItems.Where(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId).Select(oi => new
//                    {
//                        oi.ProductId,
//                        ProductName = oi.Product!.Name,
//                        ImageUrl = oi.Product.ImageUrl,
//                        oi.Price,
//                        oi.Quantity
//                    })
//                })
//                .ToListAsync();

//            return Ok(orders);
//        }

//        [HttpPut("{orderId:int}/status")]
//        [Authorize(Roles = "Seller,Admin")]
//        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
//        {
//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
//            {
//                return Unauthorized(new { message = "المستخدم غير معرف" });
//            }

//            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
//            if (seller == null && !User.IsInRole("Admin")) 
//                return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

//            var order = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .FirstOrDefaultAsync(o => o.OrderId == orderId);

//            if (order == null) return NotFound(new { message = "الأوردر غير موجود" });

//            // التأكد إن التاجر له منتجات في الأوردر ده (لو مش أدمن)
//            if (!User.IsInRole("Admin") && seller != null)
//            {
//                bool hasSellerProducts = order.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId);
//                if (!hasSellerProducts)
//                {
//                    return Unauthorized(new { message = "لا تملك صلاحية تعديل هذا الأوردر لأنه لا يحتوي على أي من منتجاتك" });
//                }
//            }

//            // تعديل الحالة (Pending, Processing, Shipped, Delivered, Cancelled)
//            string[] validStatuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
//            if (!validStatuses.Contains(dto.Status))
//            {
//                return BadRequest(new { message = "حالة الأوردر غير صحيحة. الحالات المسموحة: Pending, Processing, Shipped, Delivered, Cancelled" });
//            }

//            order.Status = dto.Status;
//            await _context.SaveChangesAsync();

//            return Ok(new { message = $"تم تحديث حالة الأوردر إلى {dto.Status}", orderId = order.OrderId });
//        }

//        [HttpPost("{orderId:int}/cancel")]
//        public async Task<IActionResult> CancelOrder(int orderId)
//        {
//            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            int customerId = int.Parse(userIdString);

//            var order = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

//            if (order == null)
//                return NotFound(new { message = "الأوردر غير موجود أو لا يخص هذا المستخدم" });

//            if (order.Status == "Cancelled")
//                return BadRequest(new { message = "الأوردر متلغي بالفعل" });

//            if (order.Status == "Completed" || order.Status == "Delivered" || order.Status == "Paid")
//                return BadRequest(new { message = "لا يمكن إلغاء هذا الأوردر بعد اكتماله أو دفعه" });

//            var hasPaidPayment = await _context.Payments
//                .AnyAsync(p => p.OrderId == orderId && p.PaymentStatus == "Paid");

//            if (hasPaidPayment)
//                return BadRequest(new { message = "لا يمكن إلغاء أوردر تم دفعه" });

//            using var transaction = await _context.Database.BeginTransactionAsync();
//            try
//            {
//                order.Status = "Cancelled";

//                foreach (var item in order.OrderItems)
//                {
//                    if (item.Product != null)
//                    {
//                        item.Product.Stock += item.Quantity ?? 1;
//                    }
//                }

//                var orderedProductIds = order.OrderItems
//                    .Where(oi => oi.ProductId.HasValue)
//                    .Select(oi => oi.ProductId.Value)
//                    .ToList();

//                if (orderedProductIds.Any())
//                {
//                    var cartItemsToDelete = await _context.CartItems
//                        .Where(ci => ci.UserId == customerId // ✅ مقارنة int بـ int
//                            && ci.ProductId.HasValue
//                            && orderedProductIds.Contains(ci.ProductId.Value))
//                        .ToListAsync();

//                    _context.CartItems.RemoveRange(cartItemsToDelete);
//                }

//                await _context.SaveChangesAsync();
//                await transaction.CommitAsync();

//                return Ok(new { message = "تم إلغاء الأوردر ومسح منتجاته من السلة", orderId = order.OrderId });
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
//                return StatusCode(500, new { message = "تفاصيل المشكلة: " + realError });
//            }
//        }
//    }

//    public class PlaceOrderDto
//    {
//        public string PaymentMethod { get; set; }
//        public string? CouponCode { get; set; }

//        public int AddressId { get; set; }
//    }

//    public class UpdateOrderStatusDto
//    {
//        public string Status { get; set; }
//    }
//}
using System.Security.Claims;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly MyDbContext _context;

        public OrdersController(MyDbContext context)
        {
            _context = context;
        }

        // 1. الدالة الأساسية: إنشاء الطلب من السلة
        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            // 1. نجيب الـ ID بتاع اليوزر من التوكن 
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int customerId = int.Parse(userIdString);

            // التأكد من وجود العميل في جدول العملاء
            var existingCustomer = await _context.Customers.FindAsync(customerId);
            if (existingCustomer == null)
            {
                var newCustomerProfile = new Customer
                {
                    CustomerId = customerId
                };
                _context.Customers.Add(newCustomerProfile);
                await _context.SaveChangesAsync();
            }

            // 2. هنجيب المنتجات من CartItems مباشرة
            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == customerId)
                .ToListAsync();

            // 3. الفحوصات بتاعتنا
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest(new { message = "سلتك فاضية يا فندم، مفيش حاجة نطلبها!" });
            }

            // 4. حساب الإجمالي المبدئي والتأكد من المخزون
            decimal totalAmount = 0;
            foreach (var item in cartItems)
            {
                if (item.Product.Stock < item.Quantity)
                    return BadRequest(new { message = $"المنتج '{item.Product.Name}' مفيش منه كمية تكفي في المخزن!" });

                totalAmount += (decimal)(item.Product.Price * item.Quantity);
            }

            // 5. معالجة الكوبون
            Coupon appliedCoupon = null;
            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                appliedCoupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == dto.CouponCode);

                if (appliedCoupon == null)
                    return BadRequest(new { message = "كود الكوبون ده مش موجود!" });

                if (appliedCoupon.ExpiryDate < DateTime.Now)
                    return BadRequest(new { message = "الكوبون ده منتهي الصلاحية!" });

                if (appliedCoupon.UsedCount >= appliedCoupon.MaxUsage)
                    return BadRequest(new { message = "الكوبون ده وصل للحد الأقصى للاستخدام!" });

                // ✅ تم التأمين هنا بـ ?? 0 لحل مشكلة الـ Nullable Decimal واختفاء الإيرور
                decimal discount = appliedCoupon.IsPercentage
                    ? totalAmount * ((decimal)(appliedCoupon.DiscountPercent ?? 0) / 100)
                    : (appliedCoupon.DiscountAmount);

                totalAmount -= discount;
                if (totalAmount < 0) totalAmount = 0; // ضمان التوتال ما يبقاش سالب
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // أ. إنشاء الطلب
                var newOrder = new Order
                {
                    CustomerId = customerId,
                    AddressId = dto.AddressId,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentMethod = dto.PaymentMethod,
                    CouponId = appliedCoupon?.CouponId,
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // ب. نقل المنتجات لجدول الـ OrderItems وتقليل المخزون
                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = (int)item.ProductId,
                        Price = item.Product.Price,
                        Quantity = item.Quantity
                    };
                    _context.OrderItems.Add(orderItem);

                    item.Product.Stock -= item.Quantity;
                }

                // ج. تفريغ السلة
                _context.CartItems.RemoveRange(cartItems);

                // د. تحديث عدد استخدامات الكوبون
                if (appliedCoupon != null)
                {
                    appliedCoupon.UsedCount += 1;
                }

                // 🌟 إضافة إشعار بنجاح إنشاء الطلب للعميل
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = customerId,
                    Title = "تم تأكيد الطلب",
                    Message = $"تم استلام طلبك بنجاح، ورقم الطلب هو #{newOrder.OrderId}."
                };
                _context.Notifications.Add(notification);

                // 🌟 إضافة إشعار للتجار (Sellers) بوجود طلب جديد لمنتجاتهم
                var sellerIds = cartItems.Where(ci => ci.Product != null && ci.Product.SellerId != null)
                                         .Select(ci => ci.Product.SellerId)
                                         .Distinct()
                                         .ToList();

                foreach (var sellerId in sellerIds)
                {
                    var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.SellerId == sellerId);
                    if (seller != null)
                    {
                        var sellerNotification = new Domain.Entities.Models.Notification
                        {
                            UserId = seller.UserId,
                            Title = "طلب جديد لمنتجاتك",
                            Message = $"لقد تلقيت طلباً جديداً (طلب رقم #{newOrder.OrderId}) يحتوي على منتجات من متجرك."
                        };
                        _context.Notifications.Add(sellerNotification);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "تم تسجيل طلبك بنجاح!",
                    orderId = newOrder.OrderId,
                    totalToPay = totalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "تفاصيل المشكلة: " + realError });
            }
        }

        [HttpGet("my-orders")]
        public async Task<ActionResult> GetMyOrders()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int customerId = int.Parse(userIdString);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentMethod,
                    o.CreatedAt,
                    // ✅ تم إرجاع حقل الـ Quantity وتأمينه للفرونت إند
                    Items = o.OrderItems.Select(oi => new
                    {
                        oi.ProductId,
                        ProductName = oi.Product.Name,
                        ImageUrl = oi.Product.ImageUrl,
                        oi.Price,
                        Quantity = oi.Quantity ?? 1
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("seller-orders")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> GetSellerOrders()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف" });
            }

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (seller == null) return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Customer)
                .ThenInclude(c => c!.CustomerNavigation)
                .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentMethod,
                    o.CreatedAt,
                    CustomerName = o.Customer != null && o.Customer.CustomerNavigation != null ? o.Customer.CustomerNavigation.FullName : "Unknown",
                    o.CustomerId,
                    Items = o.OrderItems.Where(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId).Select(oi => new
                    {
                        oi.ProductId,
                        ProductName = oi.Product!.Name,
                        ImageUrl = oi.Product.ImageUrl,
                        oi.Price,
                        oi.Quantity
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPut("{orderId:int}/status")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف" });
            }

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (seller == null && !User.IsInRole("Admin"))
                return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound(new { message = "الأوردر غير موجود" });

            if (!User.IsInRole("Admin") && seller != null)
            {
                bool hasSellerProducts = order.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == seller.SellerId);
                if (!hasSellerProducts)
                {
                    return Unauthorized(new { message = "لا تملك صلاحية تعديل هذا الأوردر لأنه لا يحتوي على أي من منتجاتك" });
                }
            }

            string[] validStatuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(dto.Status))
            {
                return BadRequest(new { message = "حالة الأوردر غير صحيحة. الحالات المسموحة: Pending, Processing, Shipped, Delivered, Cancelled" });
            }

            order.Status = dto.Status;

            // 🌟 إضافة إشعار بتحديث حالة الطلب
            if (order.CustomerId.HasValue)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = order.CustomerId.Value,
                    Title = "تحديث حالة الطلب",
                    Message = $"تم تحديث حالة طلبك رقم #{order.OrderId} إلى: {dto.Status}."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"تم تحديث حالة الأوردر إلى {dto.Status}", orderId = order.OrderId });
        }

        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int customerId = int.Parse(userIdString);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

            if (order == null)
                return NotFound(new { message = "الأوردر غير موجود أو لا يخص هذا المستخدم" });

            if (order.Status == "Cancelled")
                return BadRequest(new { message = "الأوردر متلغي بالفعل" });

            if (order.Status == "Completed" || order.Status == "Delivered" || order.Status == "Paid")
                return BadRequest(new { message = "لا يمكن إلغاء هذا الأوردر بعد اكتماله أو دفعه" });

            var hasPaidPayment = await _context.Payments
                .AnyAsync(p => p.OrderId == orderId && p.PaymentStatus == "Paid");

            if (hasPaidPayment)
                return BadRequest(new { message = "لا يمكن إلغاء أوردر تم دفعه" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.Status = "Cancelled";

                // إعادة المنتجات للمخزن بأمان
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.Stock += item.Quantity ?? 1;
                    }
                }

                // 🌟 إضافة إشعار بإلغاء الطلب
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = customerId,
                    Title = "تم إلغاء الطلب",
                    Message = $"تم إلغاء طلبك رقم #{order.OrderId} وتم إعادة المنتجات للمخزن."
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "تم إلغاء الأوردر بنجاح وإعادة المنتجات للمخزن", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "تفاصيل المشكلة: " + realError });
            }
        }
    }

    public class PlaceOrderDto
    {
        public string PaymentMethod { get; set; }
        public string? CouponCode { get; set; }
        public int AddressId { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; }
    }
}