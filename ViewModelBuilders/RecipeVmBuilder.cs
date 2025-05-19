using Microsoft.AspNetCore.Http;
using RecipeSystem.Models;
using RecipeSystem.Services;
using RecipeSystem.ViewModels;

namespace RecipeSystem.ViewModelBuilders
{
    public class RecipeVmBuilder
    {
        private readonly RecipeService _recipeService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RecipeVmBuilder(RecipeService recipeService, IHttpContextAccessor httpContextAccessor)
        {
            if (recipeService == null)
            {
                throw new ArgumentNullException(nameof(recipeService));
            }
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }
            _recipeService = recipeService;
            _httpContextAccessor = httpContextAccessor;
        }

        public RecipeVm GetRecipeVm()
        {
            int userId = 0;
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true && user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) != null)
            {
                userId = int.Parse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            }

            var baseRecipes = _recipeService.GetRecipes();
            var recipes = baseRecipes.Select(r => _recipeService.GetRecipe(r.Id, userId)).ToList();

            return new RecipeVm(recipes);
        }
        public RecipeVm GetUserRecipesVm(int userId)
        {
            var userRecipes = _recipeService.GetUserRecipes(userId);
            var recipes = userRecipes.Select(ur => new Recipe
            {
                Id = ur.BaseRecipeId ?? ur.Id, 
                Name = ur.Name,
                Description = ur.Description,
                CookingTime = ur.CookingTime,
                Servings = ur.Servings,
                CategoryID = ur.CategoryID,
                ComplexityId = ur.ComplexityId,
                Photo = ur.Photo,
                RecipeIngredients = ur.RecipeIngredients?.Select(ri => new RecipeIngredient
                {
                    RecipeId = ur.BaseRecipeId ?? ur.Id,
                    IngredientId = ri.IngredientId,
                    Quantity = ri.Quantity,
                    Ingredient = ri.Ingredient
                }).ToList(),
                Steps = ur.Steps?.Select(s => new RecipeStep
                {
                    RecipeId = ur.BaseRecipeId ?? ur.Id,
                    Description = s.Description,
                    Time = s.Time
                }).ToList(),
                Category = ur.Category
            }).ToList();

            return new RecipeVm(recipes);
        }
    }
}