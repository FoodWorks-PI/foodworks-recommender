using System.Collections.Generic;

namespace ProductRecommender.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public List<ProductRating> CourseRatings { get; set; }
    }
}