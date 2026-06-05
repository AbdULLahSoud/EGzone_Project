using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace EGzone1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly MyDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatbotController(IConfiguration configuration, MyDbContext context, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskBot([FromBody] ChatRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { message = "الرسالة لا يمكن أن تكون فارغة!" });

            // 1. قراءة الـ API Key من appsettings.json
            string apiKey = _configuration["OpenRouterApiKey"]
                            ?? throw new InvalidOperationException("OpenRouter API Key غير موجود في الإعدادات.");

            // 2. جلب كل المنتجات المتاحة بتفاصيلها الكاملة من الداتابيز
            var availableProducts = await _context.Products
                .Where(p => p.IsDeleted == false && p.IsApproved == true)
                .Include(p => p.Brand)
                .Include(p => p.SubCategory)
                .Include(p => p.Specifications)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Price,
                    p.Stock,
                    p.Description,
                    BrandName    = p.Brand != null ? p.Brand.Name : null,
                    CategoryName = p.SubCategory != null ? p.SubCategory.Name : null,
                    Specs        = p.Specifications.Select(s => s.Label + ": " + s.Value).ToList()
                })
                .ToListAsync();

            // بناء نص وصفي لكل منتج
            string productsListText = string.Join("\n", availableProducts.Select(p =>
            {
                var parts = new List<string>();
                parts.Add($"- {p.Name}");
                parts.Add($"  السعر: {p.Price} جنيه");
                parts.Add($"  المخزون: {(p.Stock > 0 ? p.Stock + " قطعة" : "نفد المخزون")}");
                if (!string.IsNullOrEmpty(p.BrandName))    parts.Add($"  الماركة: {p.BrandName}");
                if (!string.IsNullOrEmpty(p.CategoryName)) parts.Add($"  الفئة: {p.CategoryName}");
                if (!string.IsNullOrEmpty(p.Description))  parts.Add($"  الوصف: {p.Description}");
                if (p.Specs.Any()) parts.Add($"  المواصفات: {string.Join(", ", p.Specs)}");
                return string.Join("\n", parts);
            }));


            // 3. بناء الـ System Prompt
            string systemPrompt = $@"أنت مساعد ذكي ولطيف لمتجر إلكتروني اسمه 'EGzone'.
مهمتك مساعدة العملاء واقتراح المنتجات المتاحة فقط في متجرنا.
الرد يجب أن يكون بأسلوب مهذب ومختصر باللغة العربية.

قواعد مهمة جداً:
1. استند فقط إلى قائمة المنتجات التالية ولا تخترع منتجات غير موجودة فيها.
2. إذا كان المخزون > 0 فالمنتج متاح للشراء، لا تقل إنه غير متوفر.
3. إذا كان المخزون = 'نفد المخزون' فقط يمكنك إخبار العميل أنه غير متوفر حالياً.
4. اذكر السعر والماركة والفئة عند اقتراح منتج.
5. لا تتحدث عن منتجات خارج القائمة.

قائمة منتجاتنا المتاحة حالياً (كل المنتجات الموافق عليها):
{productsListText}";

            // 4. بناء الـ Request Body بصيغة OpenAI-Compatible
            var requestBody = new
            {
                model = "google/gemma-4-31b-it:free", // موديل مجاني بالكامل ✅
                max_tokens = 1024,                     // نحدد الحد عشان ميحصلش مشكلة رصيد
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = dto.Message  }
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // 5. إرسال الطلب لـ OpenRouter
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://egzone.com"); // اختياري - اسم موقعك
                client.DefaultRequestHeaders.Add("X-Title", "EGzone Chatbot");          // اختياري - اسم تطبيقك

                var response = await client.PostAsync(
                    "https://openrouter.ai/api/v1/chat/completions",
                    jsonContent
                );

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode,
                        new { message = "خطأ من OpenRouter: " + responseString });
                }

                // 6. استخراج الرد بصيغة OpenAI
                using var jsonDoc = JsonDocument.Parse(responseString);
                var botReply = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new { reply = botReply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "خطأ داخلي: " + ex.Message });
            }
        }
    }

    public class ChatRequestDto
    {
        public string Message { get; set; }
    }
}
