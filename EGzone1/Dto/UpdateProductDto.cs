namespace EGzone1.Dto
{
    public class UpdateProductDto
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int SubCategoryId { get; set; }

        public string? ImageUrl { get; set; }
    }
}