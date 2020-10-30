using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.Data;

namespace ProductRecommender.Models
{
    public class CourseRating
    {
        [Key] public int RatingId { get; set; }

        public String UserId { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public float Rating { get; set; }

        // public bool IsPaid { get; set; }
    }

    public class CourseRatingMl
    {
        [ColumnName("rating_id")]
        public int RatingId { get; set; }

        [Column("user_id",TypeName = "TEXT")]
        public String UserId { get; set; }

        [Column("course_id", TypeName = "INTEGER")]
        public int CourseId { get; set; }

        [Column("rating", TypeName = "REAL")]
        public float Rating { get; set; }

        // public bool IsPaid { get; set; }
    }
}