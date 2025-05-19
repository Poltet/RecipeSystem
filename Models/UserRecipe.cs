using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSystem.Models
{
    public class UserRecipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        public int? BaseRecipeId { get; set; }
        public Recipe? BaseRecipe { get; set; }

        [Required(ErrorMessage = "Введите название рецепта")]
        [StringLength(50, ErrorMessage = "Название не должно превышать 50 символов")]
        public string Name { get; set; }

        [StringLength(255, ErrorMessage = "Описание не должно превышать 255 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Укажите время приготовления")]
        [Range(1, int.MaxValue, ErrorMessage = "Время приготовления должно быть больше 0")]
        public int CookingTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Количество порций должно быть больше 0")]
        public int Servings { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        public int CategoryID { get; set; }
        public Category? Category { get; set; }
        public int? ComplexityId { get; set; }
        public Complexity? Complexity { get; set; }

        [BindNever]
        public byte[]? Photo { get; set; }

        [BindNever]
        public List<UserRecipeIngredient>? RecipeIngredients { get; set; }

        [BindNever]
        public List<UserRecipeStep>? Steps { get; set; }
    }

    public class UserRecipeIngredient
    {
        public int UserRecipeId { get; set; }
        public UserRecipe? UserRecipe { get; set; }
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }

        [Required(ErrorMessage = "Укажите количество")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
        public decimal Quantity { get; set; }

        [StringLength(20, ErrorMessage = "Единица измерения не должна превышать 20 символов")]
        public string? Measure { get; set; }
    }

    public class UserRecipeStep
    {
        [Key]
        public int Id { get; set; }
        public int UserRecipeId { get; set; }
        public UserRecipe? UserRecipe { get; set; }
        public string Description { get; set; }
        public int Time { get; set; } 
    }
}
