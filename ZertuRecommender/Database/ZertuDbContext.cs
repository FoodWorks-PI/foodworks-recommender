using Microsoft.EntityFrameworkCore;
using ZertuRecommender.Models;

namespace ZertuRecommender.Database
{
    public class ZertuDbContext : DbContext
    {
        public ZertuDbContext(DbContextOptions<ZertuDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
        }


        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseRating> CourseRatings { get; set; }
        public DbSet<UserView> UserViews { get; set; }
    }
}