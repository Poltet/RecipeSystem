using Microsoft.EntityFrameworkCore;
using RecipeSystem.Data;
using RecipeSystem.Models;

namespace RecipeSystem.Repositories
{
    public class RecipeRepository
    {
        private readonly RecipeDbContext _context;

        public RecipeRepository(RecipeDbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
            SetDefaultRecipe();
        }

        public void SetDefaultRecipe()
        {
            if (!_context.Recipes.Any())
            {
                var recipe1 = new Recipe { Id = 1, Name = "каша", Description = "descr", CookingTime = 30, Servings = 2, CategoryID = 1, ComplexityId = 1};
                var recipe2 = new Recipe { Id = 2, Name = "суп", Description = "описание", CookingTime = 40, Servings = 3, CategoryID = 1, ComplexityId = 2};

                _context.Recipes.AddRange(recipe1, recipe2);
                _context.SaveChanges();
            }
        }

        public List<Recipe> GetRecipes()
        {
            return _context.Recipes.Include(r => r.Category).ToList();
        }
        public List<Recipe> Search(string query, int? categoryId = null, string sortOrder = null)
        {
            var recipes = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Steps)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower().Trim();
                recipes = recipes.Where(r =>
                    r.Name.ToLower().Contains(query) ||
                    (r.Description != null && r.Description.ToLower().Contains(query)) ||
                    r.RecipeIngredients.Any(ri => ri.Ingredient != null && ri.Ingredient.Name.ToLower().Contains(query))
                );
            }

            if (categoryId.HasValue)
            {
                recipes = recipes.Where(r => r.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(sortOrder))
            {
                recipes = sortOrder == "asc"
                    ? recipes.OrderBy(r => r.CookingTime)
                    : recipes.OrderByDescending(r => r.CookingTime);
            }

            return recipes.ToList();
        }

        public List<UserRecipe> UserSearch(int userId, string query, int? categoryId = null, string sortOrder = null)
        {
            var userRecipes = _context.UserRecipes
                .Include(ur => ur.Category)
                .Include(ur => ur.RecipeIngredients).ThenInclude(uri => uri.Ingredient)
                .Include(ur => ur.Steps)
                .Where(ur => ur.UserId == userId && !ur.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower().Trim();
                userRecipes = userRecipes.Where(ur =>
                    ur.Name.ToLower().Contains(query) ||
                    (ur.Description != null && ur.Description.ToLower().Contains(query)) ||
                    ur.RecipeIngredients.Any(ri => ri.Ingredient != null && ri.Ingredient.Name.ToLower().Contains(query))
                );
            }

            if (categoryId.HasValue)
            {
                userRecipes = userRecipes.Where(ur => ur.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(sortOrder))
            {
                userRecipes = sortOrder == "asc"
                    ? userRecipes.OrderBy(ur => ur.CookingTime)
                    : userRecipes.OrderByDescending(ur => ur.CookingTime);
            }

            return userRecipes.ToList();
        }

        public void AddRecipe(Recipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            if (_context.Recipes.Any(r => r.Id == recipe.Id))
                throw new InvalidOperationException("Рецепт с таким ID уже существует.");

            Console.WriteLine($"Добавление рецепта: Id={recipe.Id}, Name={recipe.Name}, CookingTime={recipe.CookingTime}");
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
        }

        public void RemoveRecipe(int id)
        {
            var recipe = _context.Recipes.Find(id);
            if (recipe == null)
                throw new KeyNotFoundException($"Рецепт с ID {id} не найден.");

            _context.Recipes.Remove(recipe);
            _context.SaveChanges();
        }

        public void EditRecipe(Recipe recipe)
        {
            var prevRecipe = _context.Recipes
                .Include(r => r.RecipeIngredients)
                .Include(r => r.Steps)
                .SingleOrDefault(r => r.Id == recipe.Id);

            if (prevRecipe == null)
            {
                throw new Exception("Рецепт не найден");
            }

            _context.Entry(prevRecipe).CurrentValues.SetValues(recipe);

            prevRecipe.RecipeIngredients.Clear();
            foreach (var ri in recipe.RecipeIngredients ?? new List<RecipeIngredient>())
            {
                if (ri.IngredientId > 0 && ri.Quantity > 0)
                {
                    prevRecipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipe.Id,
                        IngredientId = ri.IngredientId,
                        Quantity = ri.Quantity
                    });
                }
            }


            prevRecipe.Steps.Clear();
            foreach (var step in recipe.Steps ?? new List<RecipeStep>())
            {
                if (!string.IsNullOrEmpty(step.Description))
                {
                    prevRecipe.Steps.Add(new RecipeStep
                    {
                        RecipeId = recipe.Id,
                        Description = step.Description,
                        Time = step.Time
                    });
                }
            }

            _context.SaveChanges();
        }

        public Recipe? GetRecipe(int id)
        {
            return _context.Recipes.Include(r => r.Category)
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .Include(r => r.Steps)
                .SingleOrDefault(r => r.Id == id);
        }

        // Методы для работы с пользовательскими рецептами
        public UserRecipe? GetUserRecipe(int userId, int baseRecipeId)
        {
            return _context.UserRecipes
                .Include(ur => ur.Category)
                .Include(ur => ur.RecipeIngredients)
                .ThenInclude(uri => uri.Ingredient)
                .Include(ur => ur.Steps)
                .SingleOrDefault(ur => ur.UserId == userId && ur.BaseRecipeId == baseRecipeId);
        }

        public List<UserRecipe> GetUserRecipes(int userId)
        {
            var recipes = _context.UserRecipes
                .Include(ur => ur.Category)
                .Include(ur => ur.RecipeIngredients)
                .ThenInclude(uri => uri.Ingredient)
                .Include(ur => ur.Steps)
                .Where(ur => ur.UserId == userId && !ur.IsDeleted) // Исключаем удалённые
                .ToList();
            Console.WriteLine($"Найдено пользовательских рецептов для UserId={userId}: {recipes.Count}");
            foreach (var r in recipes)
            {
                Console.WriteLine($"UserRecipe: Id={r.Id}, BaseRecipeId={r.BaseRecipeId}, Name={r.Name}, IsDeleted={r.IsDeleted}");
            }
            return recipes;
        }

        public void AddUserRecipe(UserRecipe userRecipe)
        {
            if (userRecipe == null)
                throw new ArgumentNullException(nameof(userRecipe));

            if (_context.UserRecipes.Any(ur => ur.UserId == userRecipe.UserId && ur.Name == userRecipe.Name))
                throw new InvalidOperationException($"Уже есть рецепт с названием «{userRecipe.Name}».");

            if (userRecipe.RecipeIngredients != null)
            {
                var duplicateIngredients = userRecipe.RecipeIngredients
                    .GroupBy(ri => ri.IngredientId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (duplicateIngredients.Any())
                {
                    throw new InvalidOperationException($"Обнаружены дублирующиеся IngredientId: {string.Join(", ", duplicateIngredients)}");
                }
            }
  
            if (userRecipe.Steps != null)
            {
                foreach (var step in userRecipe.Steps)
                {
                    step.Id = 0;
                    step.UserRecipeId = 0;
                }
            }

            if (userRecipe.RecipeIngredients != null)
            {
                foreach (var ingredient in userRecipe.RecipeIngredients)
                {
                    ingredient.UserRecipeId = 0;
                }
            }

            _context.UserRecipes.Add(userRecipe);
            _context.SaveChanges();
        }

        public void EditUserRecipe(int id, UserRecipe userRecipe)
        {
            if (userRecipe == null)
                throw new ArgumentNullException(nameof(userRecipe));

            var prevUserRecipe = _context.UserRecipes
                .Include(ur => ur.RecipeIngredients)
                .Include(ur => ur.Steps)
                .SingleOrDefault(ur => ur.Id == id);

            if (prevUserRecipe == null)
                throw new KeyNotFoundException($"Пользовательский рецепт с ID {id} не найден.");

            prevUserRecipe.Name = userRecipe.Name;
            prevUserRecipe.Description = userRecipe.Description;
            prevUserRecipe.CookingTime = userRecipe.CookingTime;
            prevUserRecipe.Servings = userRecipe.Servings;
            prevUserRecipe.CategoryID = userRecipe.CategoryID;
            prevUserRecipe.ComplexityId = userRecipe.ComplexityId;
            prevUserRecipe.Photo = userRecipe.Photo;
            prevUserRecipe.BaseRecipeId = userRecipe.BaseRecipeId;
            prevUserRecipe.UserId = userRecipe.UserId;

            _context.UserRecipeIngredients.RemoveRange(prevUserRecipe.RecipeIngredients);
            prevUserRecipe.RecipeIngredients = userRecipe.RecipeIngredients?.Select(ri => new UserRecipeIngredient
            {
                UserRecipeId = id,
                IngredientId = ri.IngredientId,
                Quantity = ri.Quantity,
                Measure = ri.Measure
            }).ToList() ?? new List<UserRecipeIngredient>();


            _context.UserRecipeSteps.RemoveRange(prevUserRecipe.Steps);
            prevUserRecipe.Steps = userRecipe.Steps?.Select(s => new UserRecipeStep
            {
                UserRecipeId = id,
                Description = s.Description,
                Time = s.Time
            }).ToList() ?? new List<UserRecipeStep>();

            _context.SaveChanges();
        }

        public void RemoveUserRecipe(int id, int userId)
        {
            var userRecipe = _context.UserRecipes
                .SingleOrDefault(ur => ur.Id == id && ur.UserId == userId);

            if (userRecipe == null)
                throw new KeyNotFoundException($"Пользовательский рецепт с ID {id} не найден.");

            _context.UserRecipes.Remove(userRecipe);
            _context.SaveChanges();
        }

        public List<Category> GetCategories()
        {
            return _context.Categories.ToList();
        }

        public List<Ingredient> GetIngredients()
        {
            return _context.Ingredients.ToList();
        }

        public List<Complexity> GetComplexity()
        {
            var complexities = _context.Complexity.ToList();
            Console.WriteLine($"Loaded complexities: {complexities.Count} items");
            foreach (var c in complexities)
            {
                Console.WriteLine($"Complexity: Id={c.Id}, Name={c.Name}");
            }
            return complexities;
        }

        public void AddFavorite(FavoriteRecipe favorite)
        {
            if (!_context.FavoriteRecipes.Any(fr => fr.UserId == favorite.UserId && fr.RecipeId == favorite.RecipeId))
            {
                _context.FavoriteRecipes.Add(favorite);
                _context.SaveChanges();
            }
        }

        public List<Recipe> GetFavoriteRecipes(int userId)
        {
            return _context.FavoriteRecipes
                .Where(fr => fr.UserId == userId)
                .Join(_context.Recipes,
                    fr => fr.RecipeId,
                    r => r.Id,
                    (fr, r) => r)
                .Include(r => r.Category)
                .ToList();
        }
        public List<int?> GetDeletedId(int userId)
        {
            var deleted = _context.UserRecipes
                .Where(ur => ur.UserId == userId && ur.IsDeleted)
                .Select(ur => ur.BaseRecipeId)
                .ToList();
            return deleted;
        }
    }
}