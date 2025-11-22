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

namespace FitnessApp.Web.Controllers
{
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trainers (LİSTELEME - Herkes Görebilir)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Trainers.Include(t => t.Services).ToListAsync());
        }

        // --- YENİ EKLENEN KISIM: MÜSAİTLİK ARAMA ---
        // GET: Trainers/Search
        public async Task<IActionResult> Search(DateTime? searchDate, int? searchHour)
        {
            // Eğer tarih seçilmediyse boş sayfa göster
            if (searchDate == null || searchHour == null)
            {
                return View(new List<Trainer>()); 
            }

            // Seçilen tarih ve saati birleştir
            DateTime targetDate = searchDate.Value.Date.AddHours(searchHour.Value);

            // 1. O saatte DOLU olan hocaların ID'lerini bul
            var busyTrainerIds = await _context.Appointments
                .Where(a => a.Date == targetDate && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.TrainerId)
                .ToListAsync();

            // 2. Müsait olanları filtrele
            // Şart: (ID'si dolu listesinde YOK) VE (Mesai saatleri UYGUN)
            var availableTrainers = await _context.Trainers
                .Where(t => !busyTrainerIds.Contains(t.Id)) 
                .Where(t => t.WorkStartHour <= searchHour && t.WorkEndHour > searchHour)
                .Include(t => t.Services)
                .ToListAsync();

            // Seçilenleri View'a geri gönder ki ekranda kaybolmasın
            ViewBag.SelectedDate = searchDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedHour = searchHour;

            return View(availableTrainers);
        }
        // -------------------------------------------

        // GET: Trainers/Details/5 (Herkes Görebilir)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var trainer = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null) return NotFound();
            return View(trainer);
        }

        // --- ADMIN İŞLEMLERİ (KİLİTLİ) ---

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Services = _context.Services.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,FullName,Specialty,PhotoUrl,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                if (selectedServices != null)
                {
                    trainer.Services = new List<Service>();
                    foreach (var serviceId in selectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null) trainer.Services.Add(service);
                    }
                }
                _context.Add(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var trainer = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null) return NotFound();
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Specialty,PhotoUrl,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices)
        {
            if (id != trainer.Id) return NotFound();
            if (ModelState.IsValid)
            {
                var trainerToUpdate = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(t => t.Id == id);
                if (trainerToUpdate == null) return NotFound();

                trainerToUpdate.FullName = trainer.FullName;
                trainerToUpdate.Specialty = trainer.Specialty;
                trainerToUpdate.PhotoUrl = trainer.PhotoUrl;
                trainerToUpdate.WorkStartHour = trainer.WorkStartHour;
                trainerToUpdate.WorkEndHour = trainer.WorkEndHour;

                trainerToUpdate.Services.Clear();
                if (selectedServices != null)
                {
                    foreach (var serviceId in selectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null) trainerToUpdate.Services.Add(service);
                    }
                }
                _context.Update(trainerToUpdate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var trainer = await _context.Trainers.FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null) return NotFound();
            return View(trainer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return NotFound();
            try
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ViewBag.ErrorMessage = "Bu antrenörü silemezsiniz çünkü randevuları var.";
                return View(trainer);
            }
        }

        private bool TrainerExists(int id) => _context.Trainers.Any(e => e.Id == id);
    }
}