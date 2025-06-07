using System.ComponentModel.DataAnnotations;

namespace Healy.Models.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birth date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime Birthdate { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(1, 1000, ErrorMessage = "Weight must be between 1 and 1000 kg")]
        [Display(Name = "Weight (kg)")]
        public int Weight { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(1, 300, ErrorMessage = "Height must be between 1 and 300 cm")]
        [Display(Name = "Height (cm)")]
        public int Height { get; set; }
    }
}