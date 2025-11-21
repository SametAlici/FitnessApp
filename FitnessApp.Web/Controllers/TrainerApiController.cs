using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Web.Data;
using FitnessApp.Web.Models;

namespace FitnessApp.Web.Controllers
{
    // Bu bir API Controller'dır (JSON verisi döndürür)
    [Route("api/[controller]")]
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Tüm Antrenörleri Listele
        // URL: api/TrainersApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainers()
        {
            return await _context.Trainers
                                 .Include(t => t.Services) // İlişkili hizmetleri de getir
                                 .ToListAsync();
        }

        // 2. Uzmanlık Alanına Göre Filtrele (Ödevdeki LINQ Şartı)
        // URL: api/TrainersApi/filter/Yoga
        [HttpGet("filter/{specialty}")]
        public async Task<ActionResult<IEnumerable<Trainer>>> GetTrainersBySpecialty(string specialty)
        {
            // LINQ Kullanarak filtreleme yapıyoruz
            var trainers = await _context.Trainers
                                         .Where(t => t.Specialty.Contains(specialty)) 
                                         .Include(t => t.Services)
                                         .ToListAsync();

            if (trainers == null || trainers.Count == 0)
            {
                return NotFound("Bu uzmanlık alanında antrenör bulunamadı.");
            }

            return trainers;
        }
    }
}