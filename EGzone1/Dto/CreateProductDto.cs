//using Microsoft.AspNetCore.Http;

//namespace EGzone1.DTOs
//{
//    public class CreateProductDto
//    {
//        public string Name { get; set; } = null!;
//        public decimal Price { get; set; }
//        public string Description { get; set; } = null!;

//        public int CategoryId { get; set; }
//        public int SubCategoryId { get; set; }
//        public int BrandId { get; set; } // إضافة هذه الخانة في الـ Dto المبعوث من الفرونت إند

//        // استقبال عدة صور في وقت واحد
//        public List<IFormFile> ImageFiles { get; set; } = new();

//        // استقبال المواصفات (كل عنصر عبارة عن "Label:Value")
//        // مثال: "Display:6.5-inch AMOLED"
//        public List<string> Specifications { get; set; } = new();
//    }
//}using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace EGzone1.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string Description { get; set; } = null!;

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }

        // 🌟 تعديل: جعل البراند اختياري (يقبل Null إذا لم يرسله الفرونت إند)
        public int? BrandId { get; set; }

        // استقبال عدة صور في وقت واحد
        public List<IFormFile> ImageFiles { get; set; } = new();

        // استقبال المواصفات (كل عنصر عبارة عن "Label:Value")
        public List<string> Specifications { get; set; } = new();
    }
}