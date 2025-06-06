namespace Healy.Models
{
    public class CombinedHealthMetricViewModel
    {
        public HealthMetricViewModel Daily { get; set; }
        public HealthMetricViewModel Weekly { get; set; }
        public HealthMetricViewModel Monthly { get; set; }

        public CombinedHealthMetricViewModel()
        {
            Daily = new HealthMetricViewModel();
            Weekly = new HealthMetricViewModel();
            Monthly = new HealthMetricViewModel();
        }
    }
}