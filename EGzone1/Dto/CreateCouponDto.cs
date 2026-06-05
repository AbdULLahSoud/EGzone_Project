namespace EGzone1.Dto
{
    public class CreateCouponDto
    {
        public string Code { get; set; }
        public int? DiscountPercent { get; set; }       // للكوبونات النسبة المئوية (مثلاً 20 = 20%)
        public DateTime ExpiryDate { get; set; }
        public int MaxUsage { get; set; }
        
    }
}
