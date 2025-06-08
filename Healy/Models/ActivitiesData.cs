using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Healy.Models
{
    public class ActivitiesData
    {
        public ActivityCategory? Walking { get; set; }
        public ActivityCategory? Breathing { get; set; }
        public ActivityCategory? WeightLifting { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }

    public class ActivityCategory
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Recommendation { get; set; }
        public string? Time { get; set; }
    }
}