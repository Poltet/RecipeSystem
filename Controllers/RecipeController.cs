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

namespace RecipeSystem.Controllers
{
    public class RecipeController : Controller
    {
        private readonly RecipeVmBuilder _recipeVmBuilder;
        private readonly RecipeService _recipeService;
        public RecipeController(RecipeService recipeService, RecipeVmBuilder recipeVmBuilder)
        {
            _recipeVmBuilder = recipeVmBuilder;
            _recipeService = recipeService;
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
            ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name,
                Group = new SelectListGroup { Name = i.Measure?.Trim() } 
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
                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Ошибка валидации: {error.ErrorMessage}");
                    }
                }

                ModelState.Remove("Category");
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

                if (ModelState.IsValid)
                {
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

                    Console.WriteLine($"Received recipe: {(recipe != null ? recipe.Name : "null")}");
                    Console.WriteLine($"recipeIngredients: {(recipeIngredients != null ? recipeIngredients.Count : "null")}");
                    Console.WriteLine($"steps: {(steps != null ? steps.Count : "null")}");

                    recipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) /*&& s.StepNumber > 0*/ && s.Time >= 0)
                        //.OrderBy(s => s.StepNumber)
                        .Select(s => new RecipeStep
                        {
                            //StepNumber = s.StepNumber,
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<RecipeStep>();

                    _recipeService.AddRecipe(recipe);
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
            ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name,
                Group = new SelectListGroup { Name = i.Measure?.Trim() }
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
                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Ошибка валидации: {error.ErrorMessage}");
                    }
                }
                if (ModelState.IsValid)
                {

                    recipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId) // Удаляем дубликаты по IngredientId
                        .Select(g => g.First())
                        .ToList() ?? new List<RecipeIngredient>();

            
                    recipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && /*s.StepNumber > 0 &&*/ s.Time >= 0)
                        //.OrderBy(s => s.StepNumber) 
                        .ToList() ?? new List<RecipeStep>();

                    recipe.Id = id; 
                    _recipeService.EditRecipe(recipe);
                    return RedirectToAction(nameof(Index));
                }
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                return View(recipe);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при редактировании рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", recipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
    ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
    {
        Value = i.Id.ToString(),
        Text = i.Name,
        Group = new SelectListGroup { Name = i.Measure?.Trim() }
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
        public ActionResult UserCreate(UserRecipe userRecipe, List<UserRecipeIngredient> recipeIngredients, List<UserRecipeStep> steps)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Ошибка валидации: {error.ErrorMessage}");
                    }
                }

                ModelState.Remove("User");
                ModelState.Remove("Category");
                ModelState.Remove("BaseRecipe");
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

                if (ModelState.IsValid)
                {
                    userRecipe.UserId = userId;
                    userRecipe.BaseRecipeId = null;

                    userRecipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .Select(ri => new UserRecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity,
                            Measure = ri.Measure ?? _recipeService.GetIngredients().FirstOrDefault(i => i.Id == ri.IngredientId)?.Measure ?? ""
                        })
                        .ToList() ?? new List<UserRecipeIngredient>();

                    userRecipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && /*s.StepNumber > 0 &&*/ s.Time >= 0)
                        //.OrderBy(s => s.StepNumber)
                        .Select(s => new UserRecipeStep
                        {
                            //StepNumber = s.StepNumber,
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<UserRecipeStep>();

                    _recipeService.AddUserRecipe(userRecipe);
                    return RedirectToAction(nameof(Profile));
                }

                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
                userRecipe = userRecipe ?? new UserRecipe
                {
                    RecipeIngredients = new List<UserRecipeIngredient> { new UserRecipeIngredient() },
                    Steps = new List<UserRecipeStep> { new UserRecipeStep() }
                };

                ModelState.AddModelError("", $"Ошибка при добавлении рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
            var baseRecipe = _recipeService.GetRecipe(id);
            if (baseRecipe == null)
            {
                return NotFound();
            }

            var userRecipe = _recipeService.GetUserRecipe(userId, id) ?? new UserRecipe
            {
                UserId = userId,
                BaseRecipeId = id,
                Name = baseRecipe.Name,
                Description = baseRecipe.Description ?? "",
                CookingTime = baseRecipe.CookingTime,
                Servings = baseRecipe.Servings,
                CategoryID = baseRecipe.CategoryID,
                //Difficulty = baseRecipe.Difficulty ?? "",
                ComplexityId = baseRecipe.ComplexityId,
                Photo = baseRecipe.Photo,
                RecipeIngredients = baseRecipe.RecipeIngredients?.Select(ri => new UserRecipeIngredient
                {
                    IngredientId = ri.IngredientId,
                    Quantity = ri.Quantity,
                    Measure = ri.Ingredient?.Measure
                }).ToList(),
                Steps = baseRecipe.Steps?.Select(s => new UserRecipeStep
                {
                    Description = s.Description,
                    Time = s.Time
                }).ToList()
            };

            ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
            ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name,
                Group = new SelectListGroup { Name = i.Measure?.Trim() }
            });

  
            return View("UserEdit", userRecipe); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditUserRecipe(int id, UserRecipe userRecipe, List<UserRecipeIngredient> recipeIngredients, List<UserRecipeStep> steps)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                // Логирование входных данных
                Console.WriteLine($"Received UserRecipe: Id={userRecipe.Id}, Name={userRecipe.Name}, UserId={userRecipe.UserId}, BaseRecipeId={userRecipe.BaseRecipeId}");
                Console.WriteLine($"Ingredients received: {recipeIngredients?.Count ?? 0}");
                if (recipeIngredients != null)
                {
                    for (int i = 0; i < recipeIngredients.Count; i++)
                    {
                        Console.WriteLine($"Ingredient[{i}]: Id={recipeIngredients[i].IngredientId}, Quantity={recipeIngredients[i].Quantity}, Measure={recipeIngredients[i].Measure}");
                    }
                }
                Console.WriteLine($"Steps received: {steps?.Count ?? 0}");
                if (steps != null)
                {
                    for (int i = 0; i < steps.Count; i++)
                    {
                        Console.WriteLine($"Step[{i}]: Id={steps[i].Id}, Description={steps[i].Description}, Time={steps[i].Time}");
                    }
                }

                // Удаляем ошибки валидации для навигационных свойств
                ModelState.Remove("User");
                ModelState.Remove("Category");
                ModelState.Remove("BaseRecipe");
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

                if (ModelState.IsValid)
                {
                    userRecipe.UserId = userId;
                    userRecipe.BaseRecipeId = id;

  
                    userRecipe.RecipeIngredients = recipeIngredients?
                        .Where(ri => ri.Quantity > 0 && ri.IngredientId > 0)
                        .GroupBy(ri => ri.IngredientId)
                        .Select(g => g.First())
                        .Select(ri => new UserRecipeIngredient
                        {
                            IngredientId = ri.IngredientId,
                            Quantity = ri.Quantity,
                            Measure = ri.Measure ?? _recipeService.GetIngredients().FirstOrDefault(i => i.Id == ri.IngredientId)?.Measure
                        })
                        .ToList() ?? new List<UserRecipeIngredient>();

                    userRecipe.Steps = steps?
                        .Where(s => !string.IsNullOrEmpty(s.Description) && /*s.StepNumber > 0 &&*/ s.Time >= 0)
                        //.OrderBy(s => s.StepNumber)
                        .Select(s => new UserRecipeStep
                        {
                            //StepNumber = s.StepNumber,
                            Description = s.Description,
                            Time = s.Time
                        })
                        .ToList() ?? new List<UserRecipeStep>();

                    var prevRecipe = _recipeService.GetUserRecipe(userId, id);
                    if (prevRecipe != null)
                    {
                        _recipeService.EditUserRecipe(prevRecipe.Id, userRecipe);
                    }
                    else
                    {
                        userRecipe.Id = 0;
                        _recipeService.AddUserRecipe(userRecipe);
                    }
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.Name,
                    Group = new SelectListGroup { Name = i.Measure?.Trim() }
                });
                return View("UserEdit", userRecipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error editing user recipe: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", $"Ошибка при редактировании рецепта: {ex.Message}");
                ViewBag.Categories = new SelectList(_recipeService.GetCategories(), "Id", "Name", userRecipe.CategoryID);
                ViewBag.Ingredients = _recipeService.GetIngredients().Select(i => new SelectListItem
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
                var userRecipe = _recipeService.GetUserRecipe(userId, id);
                if (userRecipe == null)
                {
                    return NotFound();
                }
                _recipeService.RemoveUserRecipe(userRecipe.Id, userId);
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
            var userRecipes = userId != 0 ? _recipeService.UserSearch(userId, query, categoryId, sortOrder) : new List<UserRecipe>();

            var model = new SearchVm
            {
                Query = query,
                Recipes = recipes,
                UserRecipes = userRecipes,
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
                            stepPara.AppendChild(new Run(new Text($"{stepNumber} {step.Description} (Время: {step.Time} мин)")));
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