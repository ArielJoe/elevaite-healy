using Microsoft.AspNetCore.Mvc;

namespace Healy.Controllers
{
    public class HealthController : Controller
    {
        public IActionResult Index()
        {
            // You can pass data to the view here if needed
            var model = new HealthMetricsViewModel
            {
                HeartRate = 72,
                Sleep = 7.2,
                BloodOxygen = 98,
                Steps = 7865,
                StepsGoal = 10000,
                Calories = 1850,
                CaloriesGoal = 2500,
                WaterIntake = 1.8,
                StressLevel = 65,
                StressDescription = "Moderate"
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "Daily")
        {
            // This endpoint can be called via AJAX to get chart data
            var chartData = await GetHealthDataByPeriod(period);
            return Json(chartData);
        }

        private async Task<object> GetHealthDataByPeriod(string period)
        {
            // Simulate getting data from database
            // Replace this with actual database calls
            return period.ToLower() switch
            {
                "daily" => new
                {
                    labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                    heartRate = new[] { 68, 72, 70, 75, 69, 73, 71 },
                    steps = new[] { 8500, 9200, 7800, 8900, 7500, 9800, 8200 }
                },
                "weekly" => new
                {
                    labels = new[] { "Week 1", "Week 2", "Week 3", "Week 4" },
                    heartRate = new[] { 70, 72, 69, 73 },
                    steps = new[] { 58500, 62000, 59800, 61200 }
                },
                "monthly" => new
                {
                    labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
                    heartRate = new[] { 69, 71, 70, 72, 68, 74 },
                    steps = new[] { 248500, 252000, 239800, 261200, 255000, 258500 }
                },
                _ => new
                {
                    labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                    heartRate = new[] { 68, 72, 70, 75, 69, 73, 71 },
                    steps = new[] { 8500, 9200, 7800, 8900, 7500, 9800, 8200 }
                }
            };
        }
    }

    public class HealthMetricsViewModel
    {
        public int HeartRate { get; set; }
        public double Sleep { get; set; }
        public int BloodOxygen { get; set; }
        public int Steps { get; set; }
        public int StepsGoal { get; set; }
        public int Calories { get; set; }
        public int CaloriesGoal { get; set; }
        public double WaterIntake { get; set; }
        public int StressLevel { get; set; }
        public string StressDescription { get; set; } = string.Empty;

        public double StepsPercentage => StepsGoal > 0 ? Math.Round((double)Steps / StepsGoal * 100, 1) : 0;
        public double CaloriesPercentage => CaloriesGoal > 0 ? Math.Round((double)Calories / CaloriesGoal * 100, 1) : 0;
    }
}
