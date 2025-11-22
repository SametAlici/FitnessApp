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
    [Authorize(Roles = "Admin")] // Sadece Admin
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trainers
        public async Task<IActionResult> Index()
        {
            // Listede hocanın uzmanlıklarını da görelim
            return View(await _context.Trainers.Include(t => t.Services).ToListAsync());
        }

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // GET: Trainers/Create
        public IActionResult Create()
        {
            // Hizmetleri Checkbox için gönderiyoruz
            ViewBag.Services = _context.Services.ToList();
            return View();
        }

        // POST: Trainers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,Specialty,PhotoUrl,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                // Seçilen hizmetleri ekle
                if (selectedServices != null)
                {
                    trainer.Services = new List<Service>();
                    foreach (var serviceId in selectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null)
                        {
                            trainer.Services.Add(service);
                        }
                    }
                }

                _context.Add(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        // GET: Trainers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Hocayı ve mevcut hizmetlerini getir
            var trainer = await _context.Trainers
                .Include(t => t.Services)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null) return NotFound();

            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        // POST: Trainers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Specialty,PhotoUrl,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices)
        {
            if (id != trainer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Önce hocayı veritabanından ilişkileriyle çek
                    var trainerToUpdate = await _context.Trainers
                        .Include(t => t.Services)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (trainerToUpdate == null) return NotFound();

                    // 2. Bilgileri güncelle
                    trainerToUpdate.FullName = trainer.FullName;
                    trainerToUpdate.Specialty = trainer.Specialty;
                    trainerToUpdate.PhotoUrl = trainer.PhotoUrl;
                    trainerToUpdate.WorkStartHour = trainer.WorkStartHour;
                    trainerToUpdate.WorkEndHour = trainer.WorkEndHour;

                    // 3. Hizmet ilişkilerini güncelle (Eskileri sil, yenileri ekle)
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
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(trainer.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Services = _context.Services.ToList();
            return View(trainer);
        }

        // GET: Trainers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // POST: Trainers/Delete/5
        // POST: Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null)
            {
                return NotFound();
            }

            try
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Başarılıysa listeye dön
            }
            catch (DbUpdateException) // Veritabanı hatası olursa buraya düşer
            {
                // Kullanıcıya gösterilecek hata mesajı
                ViewBag.ErrorMessage = "Bu antrenörü silemezsiniz çünkü ona ait kayıtlı randevular bulunmaktadır. Lütfen önce randevuları iptal edin veya silin.";
                
                // Sayfayı tekrar göster (Hata mesajıyla birlikte)
                return View(trainer);
            }
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.Id == id);
        }
    }
}