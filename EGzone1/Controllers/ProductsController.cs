using EGzone1.Dto;
using EGzone1.DTOs; // تأكد من مسار الـ DTOs بتاعك
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EGzone1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(MyDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }



        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetProducts(
      [FromQuery] string? search,
      [FromQuery] int? subCategoryId,
      [FromQuery] decimal? minPrice,
      [FromQuery] decimal? maxPrice,
      [FromQuery] int page = 1,          // ✅ رقم الصفحة (يبدأ من 1)
      [FromQuery] int pageSize = 10)     // ✅ عدد العناصر في الصفحة
        {
            // التحقق من صحة قيم الـ Pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // 1. جلب البيانات مع كل العلاقات
            var query = _context.Products
                .Include(p => p.ProductReviews)
                .Include(p => p.ProductImages)
                .Include(p => p.Specifications)
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc!.Category)
                .Include(p => p.Brand) // 🌟 سحب بيانات البراند
                .AsQueryable();

            // 2. تطبيق الفلاتر
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description!.Contains(search));
            }

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // ✅ حساب إجمالي العناصر قبل الـ Pagination
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 3. تطبيق الـ Pagination ثم التحويل لـ JSON
            var products = await query
                .OrderBy(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    category = p.SubCategory!.Category!.Name,
                    subcategory = p.SubCategory!.Name,
                    brand = p.Brand != null ? p.Brand.Name : "Unknown", // 🌟 إرجاع اسم البراند
                    price = p.Price,
                    description = p.Description,
                    images = p.ProductImages.Select(img => new
                    {
                        url = baseUrl + img.ImageUrl,
                        semanticLabel = img.SemanticLabel
                    }),
                    specifications = p.Specifications.Select(s => new
                    {
                        label = s.Label,
                        value = s.Value
                    }),
                    rating = p.ProductReviews.Any()
                        ? Math.Round(p.ProductReviews.Average(r => r.Rating), 1)
                        : 0,
                    reviewsCount = p.ProductReviews.Count(),
                    inStock = true
                })
                .ToListAsync();

            // 4. الرد مع بيانات الـ Pagination
            if (totalCount == 0)
            {
                return Ok(new { message = "لم يتم العثور على منتجات", data = new List<object>() });
            }

            return Ok(new
            {
                // ✅ بيانات التنقل بين الصفحات
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                },
                data = products
            });
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        

        [AllowAnonymous]
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetById(int id)
        //{
        //    // 1. جلب المنتج بالـ ID مع سحب كل الجداول المربوطة بيه
        //    var product = await _context.Products
        //        .Include(p => p.ProductReviews)
        //        .Include(p => p.ProductImages)   // سحب الصور
        //        .Include(p => p.Specifications)  // سحب المواصفات
        //        .Include(p => p.SubCategory)
        //            .ThenInclude(sc => sc!.Category) // سحب الأقسام
        //        .Include(p => p.Brand)               // سحب جدول البراندز
        //        .Include(p => p.Seller)              // سحب جدول السيلر بناءً على الـ SellerID
        //        .Where(p => p.ProductId == id)   // فلترة بالـ ID المطلوب فقط
        //        .Select(p => new
        //        {
        //            id = p.ProductId,
        //            name = p.Name,
        //            category = p.SubCategory!.Category!.Name,
        //            subcategory = p.SubCategory!.Name,
        //            brand = p.Brand != null ? p.Brand.Name : "Unknown",

        //            // [تعديل] تغيير p.Seller.Name إلى p.Seller.StoreName بناءً على جدول قاعدة البيانات المرفق
        //            sellerName = p.Seller != null ? p.Seller.StoreName : "Unknown",

        //            price = p.Price,
        //            description = p.Description,


        //            images = p.ProductImages.Select(img => new
        //            {
        //                // التعديل: استخدام الـ String Interpolation لضمان بناء الرابط بشكل سليم تماماً
        //                url = $"{Request.Scheme}://{Request.Host}{img.ImageUrl}",
        //                semanticLabel = img.SemanticLabel
        //            }),
        //            // تحويل لستة المواصفات للشكل المطلوب
        //            specifications = p.Specifications.Select(s => new
        //            {
        //                label = s.Label,
        //                value = s.Value
        //            }),

        //            // حساب متوسط التقييم
        //            rating = p.ProductReviews.Any()
        //                ? Math.Round(p.ProductReviews.Average(r => r.Rating), 1)
        //                : 0,

        //            reviewsCount = p.ProductReviews.Count(),
        //            inStock = true
        //        })
        //        .FirstOrDefaultAsync(); // جلب أول عنصر يطابق الـ ID

        //    // 2. لو المنتج مش موجود في الداتا بيز
        //    if (product == null)
        //    {
        //        return NotFound(new { message = "هذا المنتج غير موجود" });
        //    }

        //    // 3. إرجاع البيانات كاملة
        //    return Ok(product);
        //}
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // 1. جلب المنتج بالـ ID مع سحب كل الجداول المربوطة بيه
            var product = await _context.Products
                .Include(p => p.ProductReviews)
                .Include(p => p.ProductImages)   // سحب الصور
                .Include(p => p.Specifications)  // سحب المواصفات
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc!.Category) // سحب الأقسام
                .Include(p => p.Brand)               // سحب جدول البراندز
                .Include(p => p.Seller)              // سحب جدول السيلر بناءً على الـ SellerID
                .Where(p => p.ProductId == id)   // فلترة بالـ ID المطلوب فقط
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    // 🌟 تعديل أمان: حماية في حالة عدم وجود قسم رئيسي أو فرعي متاح
                    category = p.SubCategory != null && p.SubCategory.Category != null ? p.SubCategory.Category.Name : "No Category",
                    subcategory = p.SubCategory != null ? p.SubCategory.Name : "No SubCategory",
                    brand = p.Brand != null ? p.Brand.Name : "Unknown",

                    // تغيير p.Seller.Name إلى p.Seller.StoreName بناءً على جدول قاعدة البيانات المرفق
                    sellerName = p.Seller != null ? p.Seller.StoreName : "Unknown",

                    price = p.Price,
                    description = p.Description,

                    images = p.ProductImages.Select(img => new
                    {
                        url = $"{Request.Scheme}://{Request.Host}{img.ImageUrl}",
                        semanticLabel = img.SemanticLabel
                    }),
                    // تحويل لستة المواصفات للشكل المطلوب
                    specifications = p.Specifications.Select(s => new
                    {
                        label = s.Label,
                        value = s.Value
                    }),

                    // حساب متوسط التقييم
                    rating = p.ProductReviews.Any()
                        ? Math.Round(p.ProductReviews.Average(r => r.Rating), 1)
                        : 0,

                    reviewsCount = p.ProductReviews.Count(),
                    inStock = true
                })
                .FirstOrDefaultAsync(); // جلب أول عنصر يطابق الـ ID

            // 2. لو المنتج مش موجود في الداتا بيز
            if (product == null)
            {
                return NotFound(new { message = "هذا المنتج غير موجود" });
            }

            // 3. إرجاع البيانات كاملة
            return Ok(product);
        }

        [HttpGet("my-products")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetMyProducts()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
            {
                return Unauthorized(new { message = "المستخدم غير معرف" });
            }

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (seller == null)
            {
                return BadRequest(new { message = "حسابك ليس مسجلاً كتاجر" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = await _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc!.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Brand) // 🌟 سحب بيانات البراند
                .Where(p => p.SellerId == seller.SellerId && p.IsDeleted != true)
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.Name,
                    category = p.SubCategory != null && p.SubCategory.Category != null ? p.SubCategory.Category.Name : "No Category",
                    subcategory = p.SubCategory != null ? p.SubCategory.Name : "No SubCategory",
                    brand = p.Brand != null ? p.Brand.Name : "Unknown", // 🌟 إرجاع اسم البراند
                    price = p.Price,
                    stock = p.Stock,
                    isApproved = p.IsApproved,
                    createdAt = p.CreatedAt,
                    image = p.ProductImages.FirstOrDefault(i => i.IsMain) != null 
                            ? baseUrl + p.ProductImages.FirstOrDefault(i => i.IsMain)!.ImageUrl 
                            : (p.ProductImages.FirstOrDefault() != null ? baseUrl + p.ProductImages.FirstOrDefault()!.ImageUrl : null)
                })
                .OrderByDescending(p => p.createdAt)
                .ToListAsync();

            return Ok(products);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [HttpPost]
        [Authorize] // تأكيد الحماية العامة أولاً لضمان قراءة الـ Token
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            // 1. التأكد من أن القسم الفرعي ينتمي للقسم الرئيسي (Category Validation)
            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == dto.SubCategoryId && sc.CategoryId == dto.CategoryId);

            if (subCategory == null)
            {
                return BadRequest(new { message = "خطأ: هذا القسم الفرعي لا ينتمي للقسم الرئيسي المختار" });
            }

            // 🌟 [تعديل البراند الاختياري]: التحقق من وجود البراند في قاعدة البيانات فقط إذا تم إرساله
            if (dto.BrandId.HasValue)
            {
                var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == dto.BrandId.Value);
                if (!brandExists)
                {
                    return BadRequest(new { message = "خطأ: البراند المختار غير موجود بقاعدة البيانات" });
                }
            }

            // [تعديل أمني حرج]: جلب الـ UserId بأكثر من Claim لضمان التوافق التام مع الـ Token المبعوث
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("uid")?.Value
                              ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "خطأ: لم يتم التعرف على هوية المستخدم، تأكد من عمل Login وإرسال الـ Token بشكل صحيح" });
            }

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return BadRequest(new { message = "خطأ: صيغة معرف المستخدم في الـ Token غير صحيحة" });
            }

            // [جلب السيلر]: البحث عن التاجر المرتبط بهذا المستخدم لمنع الـ Null
            var dbSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (dbSeller == null)
            {
                return BadRequest(new { message = "خطأ: حسابك الحالي مسجل كمستخدم عادي وليس كتاجر (Seller) في قاعدة البيانات" });
            }

            // لو الأدمن هو اللي أضاف المنتج، يتعتمد فوراً
            bool isAdmin = User.IsInRole("Admin");

            // 2. إنشاء كائن المنتج وربطه بالـ SellerId الصريح
            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                SubCategoryId = dto.SubCategoryId,
                BrandId = dto.BrandId, // 🌟 سيتم حفظه كـ int أو null بناءً على ما أرسله الفرونت إند
                SellerId = dbSeller.SellerId,
                IsApproved = isAdmin, // الأدمن: معتمد فوراً، التاجر: ينتظر الموافقة
                CreatedAt = DateTime.Now
            };

            // 3. معالجة رفع الصور المتعددة بشكل نقي
            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in dto.ImageFiles)
                {
                    string fileExtension = Path.GetExtension(file.FileName);
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    product.ProductImages.Add(new ProductImage
                    {
                        ImageUrl = "/images/" + uniqueFileName,
                        SemanticLabel = dto.Name + " view"
                    });
                }
            }

            // 4. معالجة المواصفات التقنية (Label:Value)
            if (dto.Specifications != null && dto.Specifications.Any())
            {
                foreach (var spec in dto.Specifications)
                {
                    var parts = spec.Split(':');
                    if (parts.Length == 2)
                    {
                        product.Specifications.Add(new ProductSpecification
                        {
                            Label = parts[0].Trim(),
                            Value = parts[1].Trim()
                        });
                    }
                }
            }

            // 5. حفظ كل البيانات في قاعدة البيانات
            _context.Products.Add(product);

            // 🌟 إضافة إشعار للأدمن بوجود منتج جديد يحتاج مراجعة
            var adminsForProduct = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).ToListAsync();
            foreach (var adminId in adminsForProduct)
            {
                var notification = new Domain.Entities.Models.Notification
                {
                    UserId = adminId,
                    Title = "منتج جديد بانتظار الموافقة",
                    Message = $"قام التاجر '{dbSeller.StoreName}' بإضافة منتج جديد '{dto.Name}' من خلال ProductsController يحتاج إلى مراجعتك واعتمادك."
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // إرجاع الـ SellerId المرفق في الـ Response للتأكيد الفوري أثناء الفحص بـ Swagger
            return Ok(new
            {
                message = "تم إضافة المنتج بنجاح في القسم والصنف الصحيحين",
                productId = product.ProductId,
                savedSellerId = product.SellerId
            });
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "المنتج غير موجود" });

            // حماية: التأكد إن التاجر اللي بيعدل هو صاحب المنتج
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null || product.SellerId != seller.SellerId)
                return Unauthorized(new { message = "لا تملك صلاحية تعديل هذا المنتج!" });

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.SubCategoryId = dto.SubCategoryId;
            product.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم التعديل بنجاح!" });
        }






        [HttpDelete("{id}")]
        [Authorize] // الدالة محمية وتطلب Token
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // 1. جلب المنتج الأساسي من قاعدة البيانات
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "المنتج غير موجود" });

            // 2. التحقق هل المستخدم الحالي هو "أدمن"؟
            bool isAdmin = User.IsInRole("Admin");

            // 3. لو مش أدمن، نتأكد إنه التاجر صاحب المنتج نفسه
            if (!isAdmin)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "المستخدم غير معرف" });
                }

                var userId = int.Parse(userIdClaim);
                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null || product.SellerId != seller.SellerId)
                {
                    return Unauthorized(new { message = "لا تملك صلاحية مسح هذا المنتج!" });
                }
            }

            // 🌟 [التحويل إلى الحذف الناعم - Soft Delete] 🌟

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow; // تسجيل وقت الحذف اختياري لو عندك العمود ده

            // تحديث حالة المنتج في الـ Context بدل الـ Remove
            _context.Products.Update(product);

            // حفظ التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إخفاء المنتج وحذفه (Soft Delete) بنجاح دون التأثير على البيانات القديمة!" });
        }





        // 6. رفع الصورة (شابوه ليك إنك فاصلها لوحدها 👏)
        [AllowAnonymous]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "لم يتم إرسال أي صورة!" });

            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "images");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var imageUrl = $"/images/{uniqueFileName}";
            return Ok(new { url = imageUrl });
        }
    }







}

