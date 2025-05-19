namespace RecipeSystem.Models
{
    public class Complexity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<Recipe> Recipes { get; set; }
        public virtual List<UserRecipe> UserRecipes { get; set; }
    }
}
