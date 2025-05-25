using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RecipeSystem.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название рецепта")]
        [StringLength(50, ErrorMessage = "Название не должно превышать 50 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        public string? Description { get; set; } // nullable

        [Required(ErrorMessage = "Укажите время приготовления")]
        [Range(1, int.MaxValue, ErrorMessage = "Время приготовления должно быть больше 0")]
        public int CookingTime { get; set; }

        //[Required(ErrorMessage = "Укажите количество порций")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество порций должно быть больше 0")]
        public int Servings { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        public int CategoryID { get; set; } 

        public int? ComplexityId { get; set; }
        public Complexity? Complexity { get; set; }

        [BindNever]
        public byte[]? Photo { get; set; }
        [NotMapped]
        public IFormFile PhotoFile { get; set; } // Для загрузки файла
        [BindNever]
        public List<RecipeIngredient>? RecipeIngredients { get; set; }

        [BindNever]
        public List<RecipeStep>? Steps { get; set; } 
        [BindNever]
        public Category? Category { get; set; }
        [BindNever]
        public List<FavoriteRecipe>? FavoriteRecipes { get; set; }
        [BindNever]
        public List<UserRecipe>? UserRecipes { get; set; } 

        public Recipe() { }
        public Recipe(int id, string name, string? description)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }
    }

    public class RecipeIngredient
    {
        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; } 
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }

        [Required(ErrorMessage = "Укажите количество")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
        public decimal Quantity { get; set; }
    }
}