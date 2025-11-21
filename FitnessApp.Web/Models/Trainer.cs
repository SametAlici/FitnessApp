using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Web.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Uzmanlık Alanı")]
        public string Specialty { get; set; }

        public string? PhotoUrl { get; set; }

        public ICollection<Service>? Services { get; set; }
    }
}