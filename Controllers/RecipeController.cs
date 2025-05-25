using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeSystem.ViewModelBuilders;
using RecipeSystem.Services;
using RecipeSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeSystem.ViewModels;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using RecipeSystem.Data;
using DocumentFormat.OpenXml.InkML;

namespace RecipeSystem.Controllers
{
    public class RecipeController : Controller
    {
        private readonly RecipeVmBuilder _recipeVmBuilder;
        private readonly RecipeService _recipeService;
        private readonly RecipeDbContext _context;
        public RecipeController(RecipeService recipeService, RecipeVmBuilder recipeVmBuilder, RecipeDbContext context)
        {
            _recipeVmBuilder = recipeVmBuilder;
            _recipeService = recipeService;
            _context = context;
        }

        // GET: RecipeController
        public ActionResult Index()
        {
            var recipeVm = _recipeVmBuilder.GetRecipeVm();
            return View(recipeVm);
        }

        // GET: RecipeController/Details/5
        public ActionResult Details(int id)
        {
            int userId;
            if (User?.Identity?.IsAuthenticated == true && User.FindFirst(ClaimTypes.NameIdentifier) != null)
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            }
            else
            {
                userId = 0;
            }

            var recipe = _recipeService.GetRecipe(id, userId);
            if (recipe == null)
            {
                return NotFound();
            }
            return View(recipe);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name");
            ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name");
            ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name)
                .Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            });
            var model = new Recipe
            {
                RecipeIngredients = new List<RecipeIngredient> { new RecipeIngredient() },
                Steps = new List<RecipeStep> { new RecipeStep() }
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(Recipe recipe, List<RecipeIngredient> recipeIngredients, List<RecipeStep> steps)
        {
            try
            {
                ModelState.Remove("Category");
                ModelState.Remove("PhotoFile");
                foreach (var key in ModelState.Keys.Where(k => k.Contains("RecipeIngredients") && k.Contains("Recipe")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("RecipeIngredients") && k.Contains("Ingredient")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("Steps") && k.Contains("Recipe")))
                {
                    ModelState.Remove(key);
                }
                if (recipeIngredients != null)
                {
                    var duplicate = recipeIngredients
                        .GroupBy(ri => ri.IngredientId)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicate.Any())
                    {
                        ModelState.AddModelError("", $"Этот ингредиент уже есть: {duplicate[0]}");
                    }
                }
                if (recipe.PhotoFile != null && recipe.PhotoFile.Length > 0)
                {
                    if (!recipe.PhotoFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("PhotoFile", "Загружайте изображения только в формате JPEG или PNG.");
                    }
                    else
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            recipe.PhotoFile.CopyTo(memoryStream);
                            recipe.Photo = memoryStream.ToArray();
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState Is Valid");
                    recipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .Select(ri => new RecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity
                        })
                        .ToList() ?? new List<RecipeIngredient>();

                    recipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && s.Time >= 0)
                        .Select(s => new RecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<RecipeStep>();

                    _recipeService.AddRecipe(recipe);
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                recipe.RecipeIngredients = recipeIngredients ?? new List<RecipeIngredient> { new RecipeIngredient() };
                recipe.Steps = steps ?? new List<RecipeStep> { new RecipeStep() };
                return View(recipe);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при добавлении рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                recipe.RecipeIngredients = recipeIngredients ?? new List<RecipeIngredient> { new RecipeIngredient() };
                recipe.Steps = steps ?? new List<RecipeStep> { new RecipeStep() };
                return View(recipe);
            }
        }

        // GET: RecipeController/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var recipe = _recipeService.GetRecipe(id);
            if (recipe == null)
            {
                return NotFound();
            }
            ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
            ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", recipe.ComplexityId);
            ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            });
            return View(recipe);
        }

        // POST: RecipeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id, Recipe recipe, List<RecipeIngredient> recipeIngredients, List<RecipeStep> steps)
        {
            try
            {
                ModelState.Remove("PhotoFile");
                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState Is Valid");
                    var prevRecipe = _recipeService.GetRecipe(id);
                    if (prevRecipe == null)
                    {
                        return NotFound();
                    }
                    if (recipe.PhotoFile != null && recipe.PhotoFile.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            recipe.PhotoFile.CopyTo(memoryStream);
                            recipe.Photo = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        recipe.Photo = prevRecipe.Photo;
                    }
                    recipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .ToList() ?? new List<RecipeIngredient>();

                    recipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && s.Time >= 0)
                        .ToList() ?? new List<RecipeStep>();

                    recipe.Id = id;
                    _recipeService.EditRecipe(recipe);
                    return RedirectToAction(nameof(Index));
                }
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", recipe.ComplexityId);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name
                });
                return View(recipe);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при редактировании рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", recipe.ComplexityId);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                return View(recipe);
            }
        }

        // GET: RecipeController/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            try
            {
                var recipe = _recipeService.GetRecipe(id);
                if (recipe == null)
                {
                    return NotFound();
                }
                _recipeService.RemoveRecipe(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize]
        public ActionResult UserCreate()
        {
            ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name");
            ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name");
            ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            });
            var model = new UserRecipe
            {
                RecipeIngredients = new List<UserRecipeIngredient> { new UserRecipeIngredient() },
                Steps = new List<UserRecipeStep> { new UserRecipeStep() }
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> UserCreate(UserRecipe userRecipe, List<UserRecipeIngredient> recipeIngredients, List<UserRecipeStep> steps)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                ModelState.Remove("User");
                ModelState.Remove("Category");
                ModelState.Remove("BaseRecipe");
                ModelState.Remove("PhotoFile");
                foreach (var key in ModelState.Keys.Where(k => k.Contains("RecipeIngredients") && k.Contains("UserRecipe")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("RecipeIngredients") && k.Contains("Ingredient")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("Steps") && k.Contains("UserRecipe")))
                {
                    ModelState.Remove(key);
                }

                if (recipeIngredients != null)
                {
                    var duplicate = recipeIngredients
                        .GroupBy(ri => ri.IngredientId)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicate.Any())
                    {
                        ModelState.AddModelError("", $"Этот ингредиент уже есть: {duplicate[0]}");
                    }
                }

                if (userRecipe.PhotoFile != null && userRecipe.PhotoFile.Length > 0)
                {
                    if (!userRecipe.PhotoFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("PhotoFile", "Загружайте изображения только в формате JPEG или PNG.");
                    }
                    else
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await userRecipe.PhotoFile.CopyToAsync(memoryStream);
                            userRecipe.Photo = memoryStream.ToArray();
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState Is Valid");
                    userRecipe.UserId = userId;
                    userRecipe.BaseRecipeId = null;

                    userRecipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .Select(ri => new UserRecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity
                        })
                        .ToList() ?? new List<UserRecipeIngredient>();

                    userRecipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && s.Time >= 0)
                        .Select(s => new UserRecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<UserRecipeStep>();

                    _recipeService.AddUserRecipe(userRecipe);
                    return RedirectToAction(nameof(Profile));
                }

                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                userRecipe.RecipeIngredients = recipeIngredients ?? new List<UserRecipeIngredient> { new UserRecipeIngredient() };
                userRecipe.Steps = steps ?? new List<UserRecipeStep> { new UserRecipeStep() };
                return View(userRecipe);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при добавлении рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                userRecipe.RecipeIngredients = recipeIngredients ?? new List<UserRecipeIngredient> { new UserRecipeIngredient() };
                userRecipe.Steps = steps ?? new List<UserRecipeStep> { new UserRecipeStep() };
                return View(userRecipe);
            }
        }

        // GET: RecipeController/EditUserRecipe/5 
        [Authorize]
        public ActionResult EditUserRecipe(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var userRecipe = _recipeService.GetUserRecipes(userId).FirstOrDefault(ur => ur.Id == id);
            if (userRecipe == null)
            {
                userRecipe = _recipeService.GetUserRecipe(userId, id);
                if (userRecipe == null)
                {
                    var baseRecipe = _recipeService.GetRecipe(id);
                    if (baseRecipe == null)
                    {
                        return NotFound();
                    }
                    userRecipe = new UserRecipe
                    {
                        UserId = userId,
                        BaseRecipeId = id,
                        Name = baseRecipe.Name,
                        Description = baseRecipe.Description ?? "",
                        CookingTime = baseRecipe.CookingTime,
                        Servings = baseRecipe.Servings,
                        CategoryID = baseRecipe.CategoryID,
                        ComplexityId = baseRecipe.ComplexityId,
                        Photo = baseRecipe.Photo,
                        RecipeIngredients = baseRecipe.RecipeIngredients?.Select(ri => new UserRecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity,
                        }).ToList(),
                        Steps = baseRecipe.Steps?.Select(s => new UserRecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        }).ToList()
                    };
                }
            }

            ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
            ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", userRecipe.ComplexityId);
            ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name)
                .Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"{i.Name} ({i.Measure?.Trim() ?? "шт"})"
                });

            return View("UserEdit", userRecipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> EditUserRecipe(int id, UserRecipe userRecipe, List<UserRecipeIngredient> recipeIngredients, List<UserRecipeStep> steps)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                ModelState.Remove("User");
                ModelState.Remove("Category");
                ModelState.Remove("BaseRecipe");
                ModelState.Remove("PhotoFile");
                foreach (var key in ModelState.Keys.Where(k => k.Contains("recipeIngredients") && k.Contains("UserRecipe")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("recipeIngredients") && k.Contains("Ingredient")))
                {
                    ModelState.Remove(key);
                }
                foreach (var key in ModelState.Keys.Where(k => k.Contains("steps") && k.Contains("UserRecipe")))
                {
                    ModelState.Remove(key);
                }

                if (recipeIngredients != null)
                {
                    var duplicate = recipeIngredients
                        .GroupBy(ri => ri.IngredientId)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicate.Any())
                    {
                        ModelState.AddModelError("", $"Этот ингредиент уже есть: {duplicate[0]}");
                    }
                }

                // Поиск существующего пользовательского рецепта
                var prevRecipe = _recipeService.GetUserRecipes(userId).FirstOrDefault(ur => ur.Id == id);
                if (prevRecipe == null)
                {
                    prevRecipe = _recipeService.GetUserRecipe(userId, id);
                }

                var baseRecipe = prevRecipe == null ? _recipeService.GetRecipe(id) : null;
                if (prevRecipe == null && baseRecipe == null)
                {
                    return NotFound();
                }

                // Обработка фото
                if (userRecipe.PhotoFile != null && userRecipe.PhotoFile.Length > 0)
                {
                    if (!userRecipe.PhotoFile.ContentType.StartsWith("image/"))
                    {
                        ModelState.AddModelError("PhotoFile", "Загрузите изображение в формате JPEG или PNG.");
                    }
                    else
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await userRecipe.PhotoFile.CopyToAsync(memoryStream);
                            userRecipe.Photo = memoryStream.ToArray();
                        }
                    }
                }
                else
                {
                    userRecipe.Photo = prevRecipe?.Photo ?? baseRecipe?.Photo;                 
                }

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState Is Valid");
                    userRecipe.UserId = userId;
                    userRecipe.BaseRecipeId = prevRecipe?.BaseRecipeId ?? baseRecipe?.Id;

                    userRecipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .Select(ri => new UserRecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity
                        })
                        .ToList() ?? new List<UserRecipeIngredient>();

                    userRecipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && s.Time >= 0)
                        .Select(s => new UserRecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<UserRecipeStep>();

                    if (prevRecipe != null)
                    {
                        Console.WriteLine($"Редактирование рецепта с Id: {prevRecipe.Id}");
                        _recipeService.EditUserRecipe(prevRecipe.Id, userRecipe);
                    }
                    else
                    {
                        Console.WriteLine("Создание нового рецепта");
                        userRecipe.Id = 0;
                        _recipeService.AddUserRecipe(userRecipe);
                    }
                    return RedirectToAction(nameof(Profile));
                }

                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", userRecipe.ComplexityId);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                return View("UserEdit", userRecipe);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при редактировании рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Complexity = new SelectList(_recipeService.GetComplexity(), "Id", "Name", userRecipe.ComplexityId);
                ViewBag.Ingredients = _recipeService.GetIngredients().OrderBy(i => i.Name).Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                return View("UserEdit", userRecipe);
            }
        }

        // POST: RecipeController/DeleteUserRecipe/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteUserRecipe(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var recipe = _recipeService.GetUserRecipes(userId).FirstOrDefault(ur => ur.BaseRecipeId == id || (ur.Id == id && ur.UserId == userId));
                if (recipe != null)
                {
                    recipe.IsDeleted = true;
                    _recipeService.EditUserRecipe(recipe.Id, recipe);
                }
                else
                {
                    var baseRecipe = _recipeService.GetRecipe(id);
                    if (baseRecipe == null)
                    {
                        TempData["ErrorMessage"] = "Рецепт не найден.";
                        return RedirectToAction(nameof(Index));
                    }

                    recipe = new UserRecipe
                    {
                        UserId = userId,
                        BaseRecipeId = id,
                        Name = baseRecipe.Name,
                        IsDeleted = true,
                        CategoryID = baseRecipe.CategoryID,
                        ComplexityId = baseRecipe.ComplexityId,
                        CookingTime = baseRecipe.CookingTime,
                        Servings = baseRecipe.Servings
                    };
                    _recipeService.AddUserRecipe(recipe);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RecipeController/AddToFavorites
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult AddToFavorites(int recipeId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized();
                }

                int userId = int.Parse(userIdClaim);
                var favorite = new FavoriteRecipe { UserId = userId, RecipeId = recipeId };
                _recipeService.AddFavorite(favorite);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при добавлении в избранное: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize]
        public ActionResult Favorites()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var favoriteRecipes = _recipeService.GetFavoriteRecipes(userId);
            var recipeVm = new RecipeSystem.ViewModels.RecipeVm(favoriteRecipes);
            return View(recipeVm);
        }

        [Authorize]
        public ActionResult Profile()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var recipeVm = _recipeVmBuilder.GetUserRecipesVm(userId);
            return View(recipeVm);
        }

        public ActionResult Search(string query, int? categoryId, string sortOrder)
        {
            int userId = User?.Identity?.IsAuthenticated == true && User.FindFirst(ClaimTypes.NameIdentifier) != null
                ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
                : 0;

            var recipes = _recipeService.Search(query, categoryId, sortOrder);

            if (userId != 0)
            {
                var userRecipes = _recipeService.UserSearch(userId, query, categoryId, sortOrder);

                // BaseRecipeId удалённых рецептов
                var deletedRecipes = _recipeService.GetDeletedId(userId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                recipes = recipes.Where(r => !deletedRecipes.Contains(r.Id)).ToList();

                foreach (var userRecipe in userRecipes)
                {
                    recipes.RemoveAll(r => r.Id == userRecipe.BaseRecipeId);
                    recipes.Add(new Recipe
                    {
                        Id = userRecipe.BaseRecipeId ?? userRecipe.Id,
                        Name = userRecipe.Name,
                        Description = userRecipe.Description,
                        CookingTime = userRecipe.CookingTime,
                        Servings = userRecipe.Servings,
                        CategoryID = userRecipe.CategoryID,
                        ComplexityId = userRecipe.ComplexityId,
                        Photo = userRecipe.Photo,
                        RecipeIngredients = userRecipe.RecipeIngredients?.Select(ri => new RecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity,
                            Ingredient = ri.Ingredient
                        }).ToList(),
                        Steps = userRecipe.Steps?.Select(s => new RecipeStep
                        {
                            Description = s.Description,
                            Time = s.Time
                        }).ToList(),
                        Category = userRecipe.Category,
                        Complexity = userRecipe.Complexity
                    });
                }
            }
            var model = new SearchVm
            {
                Query = query,
                Recipes = recipes,
                Categories = _recipeService.GetCategories().Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == categoryId
                }).ToList(),
                SelectedCategoryId = categoryId,
                SortOrder = sortOrder
            };

            model.Categories.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Все категории",
                Selected = !categoryId.HasValue
            });

            return View(model);
        }

        public IActionResult DownloadDocx(int id)
        {
            int userId = User?.Identity?.IsAuthenticated == true && User.FindFirst(ClaimTypes.NameIdentifier) != null
                ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
                : 0;

            var recipe = _recipeService.GetRecipe(id, userId);
            if (recipe == null)
            {
                return NotFound();
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    Paragraph title = body.AppendChild(new Paragraph());
                    Run titleRun = title.AppendChild(new Run());
                    titleRun.AppendChild(new Text(recipe.Name));
                    titleRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "28" });

                    if (!string.IsNullOrEmpty(recipe.Description))
                    {
                        Paragraph desc = body.AppendChild(new Paragraph());
                        desc.AppendChild(new Run(new Text("Описание: " + recipe.Description)));
                    }

                    Paragraph time = body.AppendChild(new Paragraph());
                    time.AppendChild(new Run(new Text($"Время приготовления: {recipe.CookingTime} минут")));

                    Paragraph servings = body.AppendChild(new Paragraph());
                    servings.AppendChild(new Run(new Text($"Количество порций: {recipe.Servings}")));

                    if (recipe.Category != null)
                    {
                        Paragraph category = body.AppendChild(new Paragraph());
                        category.AppendChild(new Run(new Text($"Категория: {recipe.Category.Name}")));
                    }

                    if (recipe.Complexity != null)
                    {
                        Paragraph complexity = body.AppendChild(new Paragraph());
                        complexity.AppendChild(new Run(new Text($"Сложность: {recipe.Complexity.Name}")));
                    }

                    Paragraph ingredientsHeader = body.AppendChild(new Paragraph());
                    ingredientsHeader.AppendChild(new Run(new Text("Ингредиенты:"))).RunProperties = new RunProperties(new Bold());
                    if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
                    {
                        foreach (var ingredient in recipe.RecipeIngredients)
                        {
                            Paragraph ingredientPara = body.AppendChild(new Paragraph());
                            ingredientPara.AppendChild(new Run(new Text($"{ingredient.Quantity} {ingredient.Ingredient?.Measure} {ingredient.Ingredient?.Name}")));
                        }
                    }

                    Paragraph stepsHeader = body.AppendChild(new Paragraph());
                    stepsHeader.AppendChild(new Run(new Text("Шаги приготовления:"))).RunProperties = new RunProperties(new Bold());
                    if (recipe.Steps != null && recipe.Steps.Any())
                    {
                        int stepNumber = 1;
                        foreach (var step in recipe.Steps/*.OrderBy(s => s.StepNumber)*/)
                        {
                            Paragraph stepPara = body.AppendChild(new Paragraph());
                            stepPara.AppendChild(new Run(new Text($"{stepNumber} {step.Description} ( {step.Time} мин)")));
                            stepNumber++;
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{recipe.Name}.docx");
            }
        }
    }
}