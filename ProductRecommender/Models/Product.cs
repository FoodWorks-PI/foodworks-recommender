using System.Collections.Generic;

namespace ProductRecommender.Models
{
    public class Product
    {
        public int Id { get; set; }
        public List<ProductRating> ProductRatings { get; set; }
    }
}