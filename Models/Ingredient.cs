using System.ComponentModel.DataAnnotations;

namespace RecipeSystem.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите ингредиент")]
        [StringLength(50, ErrorMessage = "Название ингредиента не должно превышать 50 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Укажите единицу измерения")]
        [StringLength(50, ErrorMessage = "Единица измерения не должна превышать 50 символов")]
        public string Measure { get; set; }
    }
}