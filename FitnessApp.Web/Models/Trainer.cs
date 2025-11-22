using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Web.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı zorunludur.")]
        [Display(Name = "Uzmanlık Alanı")]
        public string Specialty { get; set; } // Örn: Pilates, Fitness

        [Display(Name = "Fotoğraf Linki")]
        public string? PhotoUrl { get; set; }

        // --- Ödevdeki "Müsaitlik Saatleri" Şartı İçin Eklenen Alanlar ---
        
        [Display(Name = "Mesai Başlangıç (Saat)")]
        [Range(0, 23, ErrorMessage = "Saat 0 ile 23 arasında olmalıdır.")]
        public int WorkStartHour { get; set; } = 9; // Varsayılan: 09:00

        [Display(Name = "Mesai Bitiş (Saat)")]
        [Range(0, 23, ErrorMessage = "Saat 0 ile 23 arasında olmalıdır.")]
        public int WorkEndHour { get; set; } = 18; // Varsayılan: 18:00

        // ---------------------------------------------------------------

        // Bir antrenör birden çok hizmet verebilir
        public ICollection<Service>? Services { get; set; }
    }
}