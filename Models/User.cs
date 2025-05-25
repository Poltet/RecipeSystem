namespace RecipeSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; } 
        public bool IsAdmin { get; set; } 
    }

    public class FavoriteRecipe
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
    }
}