using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Web.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı gereklidir.")]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; } 

        [Display(Name = "Süre (Dakika)")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Ücret")]
        public decimal Price { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public ICollection<Trainer>? Trainers { get; set; }
    }
}