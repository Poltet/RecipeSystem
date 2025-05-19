using RecipeSystem.Models;

namespace RecipeSystem.ViewModels
{
    public class RecipeVm
    {
        public List<Recipe> Recipes { get; set; }
        public RecipeVm(List<Recipe> recipes) 
        {
            this.Recipes = recipes;
        }

    }
}
