using System;
using System.Collections.Generic;

namespace ZertuRecommender.Models
{
    
    public class UserPreferences
    {
        public String UserId { get; set; }
        public List<int> Categories { get; set; }
    }
}