// RecipeDbContext.cs
using Microsoft.EntityFrameworkCore;
using RecipeSystem.Models;

namespace RecipeSystem.Data
{
    public class RecipeDbContext : DbContext
    {
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRecipe> UserRecipes { get; set; }
        public DbSet<UserRecipeIngredient> UserRecipeIngredients { get; set; }
        public DbSet<UserRecipeStep> UserRecipeSteps { get; set; }
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        public DbSet<Complexity> Complexity { get; set; }

        public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).HasMaxLength(500);
                entity.Property(r => r.CookingTime).IsRequired();
                entity.Property(r => r.Servings).IsRequired();
                entity.Property(r => r.CategoryID).IsRequired().HasColumnName("CategoryId"); // Явно указываем имя столбца
                //entity.Property(r => r.Difficulty).HasMaxLength(50);
                entity.HasOne(r => r.Category)
                      .WithMany(c => c.Recipes)
                      .HasForeignKey(r => r.CategoryID);
                //.HasConstraintName("FK_Recipes_Categories_CategoryId");
                entity.HasOne(r => r.Complexity) // связь с Complexity
                      .WithMany(c => c.Recipes)
                      .HasForeignKey(r => r.ComplexityId);
                      //.HasConstraintName("FK_Recipes_Complexities_ComplexityId");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Name).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Measure).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<RecipeIngredient>(entity =>
            {
                entity.HasKey(ri => new { ri.RecipeId, ri.IngredientId });
                entity.Property(ri => ri.Quantity).IsRequired();
                entity.HasOne(ri => ri.Recipe).WithMany(r => r.RecipeIngredients).HasForeignKey(ri => ri.RecipeId);
                entity.HasOne(ri => ri.Ingredient).WithMany().HasForeignKey(ri => ri.IngredientId);
            });

            modelBuilder.Entity<RecipeStep>(entity =>
            {
                entity.HasKey(rs => rs.Id);
                //entity.Property(rs => rs.StepNumber).IsRequired();
                entity.Property(rs => rs.Description).IsRequired().HasMaxLength(255);
                entity.Property(rs => rs.Time).IsRequired();
                entity.HasOne(rs => rs.Recipe).WithMany(r => r.Steps).HasForeignKey(rs => rs.RecipeId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(50);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.IsAdmin).IsRequired();
            });

            modelBuilder.Entity<UserRecipe>(entity =>
            {
                entity.HasKey(ur => ur.Id);
                entity.Property(ur => ur.UserId).IsRequired();
                entity.Property(ur => ur.BaseRecipeId);
                entity.Property(ur => ur.Name).IsRequired().HasMaxLength(50);
                entity.Property(ur => ur.Description).HasMaxLength(255);
                entity.Property(ur => ur.CookingTime).IsRequired();
                entity.Property(ur => ur.Servings).IsRequired();
                entity.Property(ur => ur.CategoryID).IsRequired().HasColumnName("CategoryId");
                //entity.Property(ur => ur.Difficulty).HasMaxLength(20);
                entity.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ur => ur.BaseRecipe).WithMany(r => r.UserRecipes).HasForeignKey(ur => ur.BaseRecipeId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ur => ur.BaseRecipe).WithMany(r => r.UserRecipes).HasForeignKey(ur => ur.BaseRecipeId);
                entity.HasOne(ur => ur.Category).WithMany().HasForeignKey(ur => ur.CategoryID);
                entity.HasOne(ur => ur.Complexity).WithMany(c => c.UserRecipes).HasForeignKey(ur => ur.ComplexityId);
            });

            modelBuilder.Entity<UserRecipeIngredient>(entity =>
            {
                entity.HasKey(uri => new { uri.UserRecipeId, uri.IngredientId });
                entity.Property(uri => uri.Quantity).IsRequired();
                entity.Property(uri => uri.Measure).HasMaxLength(20);
                entity.HasOne(uri => uri.UserRecipe).WithMany(ur => ur.RecipeIngredients).HasForeignKey(uri => uri.UserRecipeId);
                entity.HasOne(uri => uri.Ingredient).WithMany().HasForeignKey(uri => uri.IngredientId);
            });

            modelBuilder.Entity<UserRecipeStep>(entity =>
            {
                entity.HasKey(urs => urs.Id);
                entity.Property(urs => urs.UserRecipeId).IsRequired();
                //entity.Property(urs => urs.StepNumber).IsRequired();
                entity.Property(urs => urs.Description).IsRequired().HasMaxLength(255);
                entity.Property(urs => urs.Time).IsRequired();
                entity.HasOne(urs => urs.UserRecipe).WithMany(ur => ur.Steps).HasForeignKey(urs => urs.UserRecipeId);
            });

            modelBuilder.Entity<FavoriteRecipe>(entity =>
            {
                entity.HasKey(fr => new { fr.UserId, fr.RecipeId });
                entity.HasOne(fr => fr.User).WithMany().HasForeignKey(fr => fr.UserId);
                entity.HasOne(fr => fr.Recipe).WithMany(r => r.FavoriteRecipes).HasForeignKey(fr => fr.RecipeId);
            });
        }
    }
}