using RecipeSystem.Models;
using RecipeSystem.Repositories;

namespace RecipeSystem.Services
{
    public class RecipeService
    {
        private readonly RecipeRepository _recipeRepository;

        public RecipeService(RecipeRepository recipeRepository)
        {
            _recipeRepository = recipeRepository;
        }

        public List<Recipe> GetRecipes()
        {
            return _recipeRepository.GetRecipes();
        }

        public List<Recipe> Search(string query, int? categoryId = null, string sortOrder = null)
        {
            return _recipeRepository.Search(query, categoryId, sortOrder);
        }

        public List<UserRecipe> UserSearch(int userId, string query, int? categoryId = null, string sortOrder = null)
        {
            return _recipeRepository.UserSearch(userId, query, categoryId, sortOrder);
        }

        public void AddRecipe(Recipe recipe)
        {
            _recipeRepository.AddRecipe(recipe);
        }

        public void RemoveRecipe(int id)
        {
            _recipeRepository.RemoveRecipe(id);
        }

        public void EditRecipe(Recipe recipe)
        {
            _recipeRepository.EditRecipe(recipe);
        }

        public Recipe GetRecipe(int id, int userId = 0)
        {
            var recipe = _recipeRepository.GetRecipe(id);
            if (recipe == null)
                return null;

            if (userId != 0)
            {
                var userRecipe = _recipeRepository.GetUserRecipe(userId, id);
                if (userRecipe != null)
                {
                    recipe.Name = userRecipe.Name;
                    recipe.Description = userRecipe.Description;
                    recipe.CookingTime = userRecipe.CookingTime;
                    recipe.Servings = userRecipe.Servings;
                    recipe.CategoryID = userRecipe.CategoryID;
                    //recipe.Difficulty = userRecipe.Difficulty;
                    recipe.ComplexityId = userRecipe.ComplexityId;
                    recipe.Photo = userRecipe.Photo;
                }
            }
            return recipe;
        }

        public UserRecipe GetUserRecipe(int userId, int baseRecipeId)
        {
            return _recipeRepository.GetUserRecipe(userId, baseRecipeId);
        }

        public List<UserRecipe> GetUserRecipes(int userId)
        {
            return _recipeRepository.GetUserRecipes(userId);
        }

        public void AddUserRecipe(UserRecipe userRecipe)
        {
            _recipeRepository.AddUserRecipe(userRecipe);
        }

        public void EditUserRecipe(int id, UserRecipe userRecipe)
        {
            _recipeRepository.EditUserRecipe(id, userRecipe);
        }

        public void RemoveUserRecipe(int id, int userId)
        {
            _recipeRepository.RemoveUserRecipe(id, userId);
        }

        public List<Category> GetCategories()
        {
            return _recipeRepository.GetCategories();
        }
        public List<Complexity> GetComplexity()
        {
            return _recipeRepository.GetComplexity();
        }
        public List<Ingredient> GetIngredients()
        {
            return _recipeRepository.GetIngredients();
        }

        public void AddFavorite(FavoriteRecipe favorite)
        {
            _recipeRepository.AddFavorite(favorite);
        }
        public void RemoveFavorite(int userId, int recipeId)
        {
            _recipeRepository.RemoveFavorite(userId, recipeId);
        }
        public List<Recipe> GetFavoriteRecipes(int userId)
        {
            return _recipeRepository.GetFavoriteRecipes(userId);
        }
        public List<int?> GetDeletedId(int userId)
        {
            return _recipeRepository.GetDeletedId(userId);
        }
    }
}