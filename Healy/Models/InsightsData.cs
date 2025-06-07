using System;

namespace Healy.Models
{
    public class InsightsData
    {
        public InsightCategory? HeartRate { get; set; }
        public InsightCategory? Sleep { get; set; }
        public InsightCategory? BloodO2 { get; set; }
        public InsightCategory? Steps { get; set; }
        public InsightCategory? Calories { get; set; }
        public InsightCategory? Stress { get; set; }
    }

    public class InsightCategory
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Recommendation { get; set; }
        public string? Time { get; set; }
    }
}