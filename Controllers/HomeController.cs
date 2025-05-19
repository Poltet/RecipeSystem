using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RecipeSystem.Models;
using RecipeSystem.ViewModelBuilders;

namespace RecipeSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        //private readonly RecipeVmBuilder _recipeVmBuilder;

        //public HomeController(ILogger<HomeController> logger, RecipeVmBuilder recipeVmBuilder)
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            //_recipeVmBuilder = recipeVmBuilder;
        }

        public IActionResult Index()
        {
            //var recipeVm = _recipeVmBuilder.GetRecipeVm();
            //return View(recipeVm);
            return RedirectToAction(nameof(RecipeController.Index), "Recipe");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
