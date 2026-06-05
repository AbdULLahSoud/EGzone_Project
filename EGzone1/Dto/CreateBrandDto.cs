using System.ComponentModel.DataAnnotations;

public class CreateBrandDto
{
    [Required(ErrorMessage = "اسم البراند مطلوب")]
    public string Name { get; set; } = null!;

   
    
}