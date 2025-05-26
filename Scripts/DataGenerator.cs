using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecipeSystem.Models;
using RecipeSystem.Data;

namespace RecipeSystem
{
    public static class DataGenerator
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly string _apiKey = "AQV***";

        public static async Task SeedData(RecipeDbContext context)
        {
            var existingRecipes = await context.Recipes.Select(r => r.Name.ToLower()).ToListAsync();

            // Получить статические рецепты
            var recipes = GetStaticRussianRecipes();

            foreach (var recipeData in recipes.Take(50))
            {
                if (existingRecipes.Contains(recipeData.Name.ToLower()))
                {
                    continue;
                }

                byte[] photo = null;
                try
                {
                    photo = await _client.GetByteArrayAsync(recipeData.PhotoUrl);
                }
                catch { }


                string description = await GenerateDescriptionAsync(recipeData.Name, recipeData.Category, string.Join(", ", recipeData.Ingredients.Select(i => i.Item1)));
                if (string.IsNullOrEmpty(description))
                {
                    description = "Вкусное блюдо";
                }

                var recipe = new Recipe
                {
                    Name = recipeData.Name.Substring(0, Math.Min(50, recipeData.Name.Length)),
                    Description = description.Substring(0, Math.Min(500, description.Length)),
                    CookingTime = 30,
                    Servings = 4,
                    CategoryID = recipeData.CategoryId,
                    ComplexityId = 2,
                    Photo = photo,
                    RecipeIngredients = new List<RecipeIngredient>(),
                    Steps = new List<RecipeStep>()
                };

                foreach (var (ingName, measure) in recipeData.Ingredients)
                {
                    var dbIngredient = await context.Ingredients.FirstOrDefaultAsync(i => i.Name.ToLower() == ingName.ToLower());
                    if (dbIngredient == null)
                    {
                        dbIngredient = new Ingredient
                        {
                            Name = ingName,
                            Measure = measure.Contains("г") || measure.Contains("мл") ? measure : "шт"
                        };
                        context.Ingredients.Add(dbIngredient);
                        await context.SaveChangesAsync();
                    }

                    recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        IngredientId = dbIngredient.Id,
                        Quantity = measure.Contains("г") || measure.Contains("мл") ? 1 : decimal.Parse(measure.Replace(" ", "").Replace("ч.л", ""))
                    });
                }

                var steps = recipeData.Instructions.Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.Length > 5)
                    .Take(5)
                    .ToList();
                for (int i = 0; i < steps.Count; i++)
                {
                    recipe.Steps.Add(new RecipeStep
                    {
                        Description = steps[i].Trim().Substring(0, Math.Min(255, steps[i].Length)),
                        Time = 10
                    });
                }

                context.Recipes.Add(recipe);
                try
                {
                    await context.SaveChangesAsync();
                    existingRecipes.Add(recipeData.Name.ToLower());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static async Task<string> GenerateDescriptionAsync(string name, string category, string ingredients)
        {
            try
            {
                var prompt = $"Опиши рецепт на русском. Название: {name}. Категория: {category}. Ингредиенты: {ingredients}.";
                var request = new HttpRequestMessage(HttpMethod.Post, "https://llm.api.cloud.yandex.net/foundationModels/v1/completion");
                request.Headers.Add("Authorization", $"Api-Key {_apiKey}");
                request.Headers.Add("x-folder-id", "b1***");

                var body = new
                {
                    modelUri = "gpt://b1***/yandexgpt/latest",
                    completionOptions = new { stream = false, temperature = 0.6, maxTokens = 200 },
                    messages = new[] { new { role = "user", text = prompt } }
                };

                request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseBody);
                return json.RootElement.GetProperty("result").GetProperty("alternatives")[0].GetProperty("message").GetProperty("text").GetString();
            }
            catch
            {
                return "";
            }
        }

        private static List<(string Name, string Category, int CategoryId, string Instructions, string PhotoUrl, (string Ingredient, string Measure)[] Ingredients)> GetStaticRussianRecipes()
        {
            return new List<(string, string, int, string, string, (string, string)[])>
            {
                (
                    "Борщ", "Soup", 4,
                    "Варить говядину 2 часа. Добавить свёклу, капусту, картофель, морковь, лук. Варить 30 минут. Добавить томатную пасту. Подавать со сметаной.",
                    "https://www.themealdb.com/images/media/meals/ujxmx4202009170950.jpg",
                    new[] { ("Говядина", "500 г"), ("Свёкла", "2 шт"), ("Капуста", "200 г"), ("Картофель", "3 шт"), ("Морковь", "1 шт"), ("Лук", "1 шт"), ("Томатная паста", "2 ч.л"), ("Сметана", "100 г") }
                ),
                   (
                    "Окрошка", "Soup", 4,
                    "Нарезать картофель, яйца, огурцы, редис, ветчину. Смешать с квасом и сметаной. Охладить.",
                    "https://www.themealdb.com/images/media/meals/0609c9202009170950.jpg",
                    new[] { ("Картофель", "2 шт"), ("Яйца", "3 шт"), ("Огурцы", "2 шт"), ("Редис", "100 г"), ("Ветчина", "200 г"), ("Квас", "1 л"), ("Сметана", "100g") }
                ),
                (
                    "Солянка", "Soup", 4,
                    "Обжарить колбасу, мясо, лук. Добавить огурцы, томатную пасту, маслины. Варить 20 минут. Подавать с лимоном.",
                    "https://www.themealdb.com/images/media/meals/xxpqsy1511452222.jpg",
                    new[] { ("Колбаса", "200 г"), ("Говядина", "200 г"), ("Лук", "1 шт"), ("Солёные огурцы", "2 шт"), ("Томатная паста", "2 ч.л."), ("Маслины", "50 г"), ("Лимон", "1 шт") }
                )
            };
        }
    }
}