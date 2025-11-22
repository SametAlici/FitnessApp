using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Web.Data;
using FitnessApp.Web.Models;

namespace FitnessApp.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TÜM ANTRENÖRLERİ LİSTELEME
        // URL: api/TrainersApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainers()
        {
            return await _context.Trainers
                                 .Include(t => t.Services)
                                 .ToListAsync();
        }

        // 2. UZMANLIK ALANINA GÖRE FİLTRELEME (LINQ)
        // URL: api/TrainersApi/filter/Yoga
        [HttpGet("filter/{specialty}")]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainersBySpecialty(string specialty)
        {
            var trainers = await _context.Trainers
                                         .Where(t => t.Specialty.Contains(specialty))
                                         .Include(t => t.Services)
                                         .ToListAsync();

            if (trainers == null || trainers.Count == 0) return NotFound("Hoca bulunamadı.");
            return trainers;
        }

        // 3. BELİRLİ BİR TARİHTE UYGUN ANTRENÖRLERİ GETİRME (GELİŞMİŞ LINQ)
        // URL: api/TrainersApi/available?date=2025-11-25T14:00:00
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetAvailableTrainers(DateTime date)
        {
            // a. O saatte randevusu olan (DOLU) hocaların ID'lerini bul
            var busyTrainerIds = await _context.Appointments
                .Where(a => a.Date == date && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.TrainerId)
                .ToListAsync();

            // b. Şartları sağlayan hocaları filtrele:
            //    1. ID'si "Dolu Hocalar" listesinde OLMAYACAK (!Contains)
            //    2. Seçilen saat, hocanın mesai saatleri içinde OLACAK
            var availableTrainers = await _context.Trainers
                .Where(t => !busyTrainerIds.Contains(t.Id)) // LINQ: Dolu olmayanlar
                .Where(t => t.WorkStartHour <= date.Hour && t.WorkEndHour > date.Hour) // LINQ: Mesaisi uyanlar
                .Include(t => t.Services)
                .ToListAsync();

            if (availableTrainers.Count == 0) 
                return NotFound("Seçilen saatte uygun antrenör bulunamadı.");

            return availableTrainers;
        }
    }
}