using System.ComponentModel.DataAnnotations;

namespace RecipeSystem.Models
{
    public class Register
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        [StringLength(256, ErrorMessage = "Имя пользователя не должно превышать 256 символов")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}
