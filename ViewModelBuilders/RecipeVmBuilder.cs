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
            var recipes = new List<Recipe>();

            if (userId != 0)
            {
                // BaseRecipeId удалённых рецептов
                var deletedRecipes = _recipeService.GetDeletedId(userId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value).ToList();

                // Исключение удалённых рецептов
                recipes = baseRecipes
                    .Where(r => !deletedRecipes.Contains(r.Id))
                    .Select(r => _recipeService.GetRecipe(r.Id, userId)).ToList();

                // Пользовательские рецепты
                var userRecipes = _recipeService.GetUserRecipes(userId)
                    .Where(ur => ur.BaseRecipeId == null)
                    .Select(ur => new Recipe
                    {
                        Id = ur.Id,
                        Name = ur.Name,
                        Description = ur.Description,
                        CookingTime = ur.CookingTime,
                        Servings = ur.Servings,
                        CategoryID = ur.CategoryID,
                        ComplexityId = ur.ComplexityId,
                        Photo = ur.Photo,
                        RecipeIngredients = ur.RecipeIngredients?.Select(ri => new RecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity,
                            Ingredient = ri.Ingredient
                        }).ToList(),
                        Steps = ur.Steps?.Select(s => new RecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        }).ToList(),
                        Category = ur.Category,
                        Complexity = ur.Complexity
                    })
                    .ToList();

                recipes.AddRange(userRecipes);
            }
            else
            {
                recipes = baseRecipes.Select(r => _recipeService.GetRecipe(r.Id, userId)).ToList();
            }

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
                    IngredientId = ri.IngredientId,
                    Quantity = ri.Quantity,
                    Ingredient = ri.Ingredient
                }).ToList(),
                Steps = ur.Steps?.Select(s => new RecipeStep
                {
                    Description = s.Description,
                    Time = s.Time
                }).ToList(),
                Category = ur.Category,
                Complexity = ur.Complexity
            }).ToList();

            return new RecipeVm(recipes);
        }
    }
}