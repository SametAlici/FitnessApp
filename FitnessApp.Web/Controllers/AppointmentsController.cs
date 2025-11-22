using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Web.Data;
using FitnessApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace FitnessApp.Web.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        // GET: Appointments
        // searchString: Arama kutusundan gelen üye adı veya email
        public async Task<IActionResult> Index(string searchString)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // 1. Temel Sorguyu Hazırla (Henüz veritabanına gitmedi)
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Service)
                .Include(a => a.Trainer)
                .AsQueryable();

            if (User.IsInRole("Admin"))
            {
                // 2. Eğer Admin bir şey arattıysa filtrele
                if (!string.IsNullOrEmpty(searchString))
                {
                    // Email içinde VEYA İsim içinde arama yap
                    appointmentsQuery = appointmentsQuery.Where(a => 
                        a.Member.Email.Contains(searchString) || 
                        a.Member.FirstName.Contains(searchString) ||
                        a.Member.LastName.Contains(searchString));
                    
                    // Arama bilgisini ekrana geri gönder (Kutuda yazılı kalsın)
                    ViewData["CurrentFilter"] = searchString;
                }
                // Admin hepsini (veya filtrelenmiş halini) görür
            }
            else
            {
                // Normal üye SADECE kendininkini görür (Arama yapamaz)
                appointmentsQuery = appointmentsQuery.Where(a => a.MemberId == user.Id);
            }

            // 3. Tarihe göre sırala ve listeyi getir
            var result = await appointmentsQuery.OrderByDescending(a => a.Date).ToListAsync();
            
            return View(result);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Service)
                .Include(a => a.Trainer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && appointment.MemberId != user.Id) return Forbid();
            return View(appointment);
        }

        
       // GET: Appointments/Create
        public IActionResult Create()
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name");
            
            // 1. Hizmetlerin Fiyatlarını da çekip JSON yapıyoruz (YENİ)
            var services = _context.Services.ToList();
            var serviceData = services.Select(s => new 
            {
                id = s.Id,
                price = s.Price // Fiyatı buraya ekledik
            }).ToList();
            ViewBag.ServiceData = JsonSerializer.Serialize(serviceData);

            // 2. Antrenör Verileri
            var trainers = _context.Trainers.Include(t => t.Services).ToList();
            var trainerData = trainers.Select(t => new 
            {
                id = t.Id,
                fullName = t.FullName,
                serviceIds = t.Services.Select(s => s.Id).ToList()
            }).ToList();
            ViewBag.TrainerData = JsonSerializer.Serialize(trainerData);
            
            return View();
        }

        // POST: Appointments/Create
        // DİKKAT: Parametreleri değiştirdik (Tarih ve Saat ayrı geliyor)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int TrainerId, int ServiceId, DateTime selectedDate, int selectedHour)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Tarih ve Saati Birleştir
            DateTime finalDateTime = selectedDate.Date.AddHours(selectedHour); // Örn: 20.11.2025 14:00

            var appointment = new Appointment
            {
                MemberId = user.Id,
                TrainerId = TrainerId,
                ServiceId = ServiceId,
                Date = finalDateTime, // Birleşmiş tarihi atıyoruz
                Status = AppointmentStatus.Pending
            };

            // Validasyon temizliği
            ModelState.Remove("MemberId");
            ModelState.Remove("Member");
            ModelState.Remove("Status");
            ModelState.Remove("Trainer");
            ModelState.Remove("Service");
            // Appointment modelinde Date zorunlu olduğu için, onu da temizliyoruz çünkü elle atadık
            ModelState.Remove("Date"); 

            // 2. Geçmiş Tarih Kontrolü
            if (appointment.Date < DateTime.Now)
            {
                ModelState.AddModelError("Date", "Geçmiş bir tarihe randevu alamazsınız.");
            }

            // 3. Antrenör Kontrolleri
            var trainer = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(t => t.Id == TrainerId);
            
            if (trainer != null)
            {
                // Mesai Kontrolü
                if (selectedHour < trainer.WorkStartHour || selectedHour >= trainer.WorkEndHour)
                {
                    ModelState.AddModelError("Date", $"Bu antrenör sadece {trainer.WorkStartHour}:00 - {trainer.WorkEndHour}:00 saatleri arasında çalışır.");
                }

                // Uzmanlık Kontrolü
                if (!trainer.Services.Any(s => s.Id == ServiceId))
                {
                    ModelState.AddModelError("TrainerId", "Seçilen antrenör bu dersi vermemektedir.");
                }
            }
            else
            {
                ModelState.AddModelError("TrainerId", "Lütfen geçerli bir antrenör seçiniz.");
            }

            // 4. Çakışma Kontrolü (Aynı hoca, aynı tarih, aynı saat)
            bool isBusy = await _context.Appointments.AnyAsync(a => 
                a.TrainerId == TrainerId && 
                a.Date == appointment.Date && 
                a.Status != AppointmentStatus.Cancelled);

            if (isBusy)
            {
                ModelState.AddModelError("Date", "Seçilen saatte antrenörün başka bir randevusu var.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata varsa sayfayı tekrar doldur
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", ServiceId);
            
            // JSON verisini tekrar gönder
            var trainers = _context.Trainers.Include(t => t.Services).ToList();
            var trainerData = trainers.Select(t => new 
            {
                id = t.Id,
                fullName = t.FullName,
                serviceIds = t.Services.Select(s => s.Id).ToList()
            }).ToList();
            ViewBag.TrainerData = JsonSerializer.Serialize(trainerData);
            
            // Hata durumunda Trainer listesi boş gider, JS tekrar doldurur
            ViewData["TrainerId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(appointment);
        }

        // --- İPTAL İŞLEMİ ---
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments.Include(a => a.Trainer).Include(a => a.Service).FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && appointment.MemberId != user.Id) return Forbid();
            return View(appointment);
        }

        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var app = await _context.Appointments.FindAsync(id);
            if (app != null) { app.Status = AppointmentStatus.Cancelled; _context.Update(app); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        // --- ADMIN (EDIT/DELETE) ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id) {
             if (id == null) return NotFound();
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();
            ViewData["MemberId"] = new SelectList(_context.Users, "Id", "UserName", appointment.MemberId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "FullName", appointment.TrainerId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,Status,MemberId,TrainerId,ServiceId")] Appointment appointment) {
            if (id != appointment.Id) return NotFound();
            ModelState.Remove("Member"); ModelState.Remove("Trainer"); ModelState.Remove("Service");
            if (ModelState.IsValid) {
                _context.Update(appointment); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id) {
             if (id == null) return NotFound();
            var appointment = await _context.Appointments.Include(a => a.Member).Include(a => a.Service).Include(a => a.Trainer).FirstOrDefaultAsync(m => m.Id == id);
            if (appointment == null) return NotFound();
            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var app = await _context.Appointments.FindAsync(id);
            if (app != null) { _context.Appointments.Remove(app); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id) => _context.Appointments.Any(e => e.Id == id);
    }
}