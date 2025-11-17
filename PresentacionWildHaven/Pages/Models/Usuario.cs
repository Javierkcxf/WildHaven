
using System.ComponentModel.DataAnnotations;

namespace PresentacionWildHaven.Models 
{
    public class Usuario
    {
        [Required(ErrorMessage = "Full name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
        public bool AcceptTerms { get; set; }
        public string? Telefono { get; set; }
    }
}