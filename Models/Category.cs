using System.ComponentModel.DataAnnotations;

namespace RecipeSystem.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите категорию")]
        [StringLength(50, ErrorMessage = "Название категории не должно превышать 50 символов")]
        public string Name { get; set; }

        public List<Recipe> Recipes { get; set; } = new List<Recipe>(); 
    }
}