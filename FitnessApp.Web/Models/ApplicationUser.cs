using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; }

        [Display(Name = "Doğum Tarihi")]
        public DateTime? BirthDate { get; set; }

        // Yapay Zeka önerileri için
        [Display(Name = "Boy (cm)")]
        public double? Height { get; set; }

        [Display(Name = "Kilo (kg)")]
        public double? Weight { get; set; }

        public string? PhotoUrl { get; set; }
    }
}