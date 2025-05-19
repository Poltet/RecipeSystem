using RecipeSystem.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RecipeSystem.ViewModels
{
    public class SearchVm
    {
        public string Query { get; set; }
        public List<Recipe> Recipes { get; set; }
        public List<UserRecipe> UserRecipes { get; set; }
        public List<SelectListItem> Categories { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string SortOrder { get; set; } // "asc" или "desc"
    }
}