using Microsoft.EntityFrameworkCore;
using ProductRecommender.Models;

namespace ProductRecommender.Database
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
        }


        public DbSet<Product> Products { get; set; }
        public DbSet<ProductRating> ProductRatings { get; set; }
        public DbSet<UserView> UserViews { get; set; }
    }
}