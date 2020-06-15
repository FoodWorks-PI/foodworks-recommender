﻿using System.Collections.Generic;

namespace ZertuRecommender.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public int CategoryId { get; set; }
        public List<CourseRating> CourseRatings { get; set; }
        public int VideoCount { get; set; }
    }
}