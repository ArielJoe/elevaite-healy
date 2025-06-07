using System.ComponentModel.DataAnnotations;

namespace Healy.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime Birthdate { get; set; }

        [Required]
        [Range(1, 1000)]
        [Display(Name = "Weight (kg)")]
        public int Weight { get; set; }

        [Required]
        [Range(1, 300)]
        [Display(Name = "Height (cm)")]
        public int Height { get; set; }

        [Display(Name = "Wearable Data")]
        public string WearableData { get; set; } = string.Empty;

        [Display(Name = "Insights")]
        public List<string> Insights { get; set; } = new List<string>();
    }
}