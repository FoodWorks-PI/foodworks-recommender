using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.Data;

namespace ProductRecommender.Models
{
    [Table("ratings")]
    public class ProductRating
    {
        [Column("id")]
        [Key] public int RatingId { get; set; }

        [Column("customer_ratings",TypeName = "INTEGER")]
        public int UserId { get; set; }

        [Column("product_ratings", TypeName = "INTEGER")]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public float Rating { get; set; }

        // public bool IsPaid { get; set; }
    }

    public class ProductRatingMl
    {
        [ColumnName("id")]
        public int RatingId { get; set; }

        [Column("customer_ratings",TypeName = "INTEGER")]
        public int UserId { get; set; }

        [Column("product_ratings", TypeName = "INTEGER")]
        public int ProductId { get; set; }

        [Column("rating", TypeName = "INTEGER")]
        public float Rating { get; set; }

        // public bool IsPaid { get; set; }
    }
}