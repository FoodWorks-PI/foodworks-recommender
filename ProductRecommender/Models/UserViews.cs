using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.ML.Data;

namespace ProductRecommender.Models
{
    public class UserView
    {
        [Key] public int ViewId { get; set; }

        public String UserId { get; set; }

        public int CourseId { get; set; }
        public int VideoId { get; set; }
    }
}