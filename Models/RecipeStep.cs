using System.ComponentModel.DataAnnotations;

namespace RecipeSystem.Models
{
    public class RecipeStep
    {
        public int Id { get; set; }

        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Укажите описание шага")]
        [StringLength(255, ErrorMessage = "Описание шага не должно превышать 255 символов")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Укажите время выполнения шага")]
        [Range(0, int.MaxValue, ErrorMessage = "Время выполнения не может быть отрицательным")]
        public int Time { get; set; }

        public Recipe? Recipe { get; set; }
    }
}
