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

        // GET: Trainers (LİSTELEME)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Trainers.Include(t => t.Services).ToListAsync());
        }

        // --- ARAMA VE MÜSAİTLİK SORGULAMA (EKSİK OLAN KISIM) ---
        public async Task<IActionResult> Search(DateTime? searchDate, int? searchHour, string trainerName)
        {
            var query = _context.Trainers
                .Include(t => t.Services)
                .Include(t => t.Appointments.Where(a => a.Date >= DateTime.Now && a.Status != AppointmentStatus.Cancelled))
                .AsQueryable();

            // 1. İSİM İLE ARAMA
            if (!string.IsNullOrEmpty(trainerName))
            {
                query = query.Where(t => t.FullName.Contains(trainerName));
                ViewBag.SearchMode = "Name";
            }
            // 2. TARİH/SAAT İLE ARAMA
            else if (searchDate != null && searchHour != null)
            {
                DateTime targetDate = searchDate.Value.Date.AddHours(searchHour.Value);
                
                var busyTrainerIds = await _context.Appointments
                    .Where(a => a.Date == targetDate && a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.TrainerId)
                    .ToListAsync();

                query = query.Where(t => !busyTrainerIds.Contains(t.Id) && 
                                         t.WorkStartHour <= searchHour && 
                                         t.WorkEndHour > searchHour);
                
                ViewBag.SelectedDate = searchDate.Value.ToString("yyyy-MM-dd");
                ViewBag.SelectedHour = searchHour;
                ViewBag.SearchMode = "Date";
            }
            else
            {
                return View(new List<Trainer>());
            }

            return View(await query.ToListAsync());
        }
        // -------------------------------------------------------

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var trainer = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null) return NotFound();
            return View(trainer);
        }

        // --- ADMIN İŞLEMLERİ (Create/Edit/Delete) ---

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Services = _context.Services.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        // RESİM YÜKLEME İÇİN IFormFile EKLENDİ
        public async Task<IActionResult> Create([Bind("Id,FullName,Specialty,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices, IFormFile? imageFile)
        {
            ModelState.Remove("PhotoUrl"); // Validasyon hatasını önle

            if (ModelState.IsValid)
            {
                // --- RESİM YÜKLEME ---
                if (imageFile != null && imageFile.Length > 0)
                {
                    var extension = Path.GetExtension(imageFile.FileName);
                    var newImageName = Guid.NewGuid() + extension;
                    var location = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/", newImageName);

                    // Klasör yoksa oluştur
                    if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/")))
                        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/"));

                    using (var stream = new FileStream(location, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    trainer.PhotoUrl = "/images/" + newImageName;
                }
                else
                {
                    trainer.PhotoUrl = "https://cdn-icons-png.flaticon.com/512/8815/8815112.png"; // Varsayılan
                }
                // ---------------------

                // Hizmetleri Ekle
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
        // RESİM GÜNCELLEME İÇİN IFormFile EKLENDİ
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Specialty,WorkStartHour,WorkEndHour")] Trainer trainer, int[] selectedServices, IFormFile? imageFile)
        {
            if (id != trainer.Id) return NotFound();
            ModelState.Remove("PhotoUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    var trainerToUpdate = await _context.Trainers.Include(t => t.Services).FirstOrDefaultAsync(t => t.Id == id);
                    if (trainerToUpdate == null) return NotFound();

                    // Bilgileri Güncelle
                    trainerToUpdate.FullName = trainer.FullName;
                    trainerToUpdate.Specialty = trainer.Specialty;
                    trainerToUpdate.WorkStartHour = trainer.WorkStartHour;
                    trainerToUpdate.WorkEndHour = trainer.WorkEndHour;

                    // --- RESİM GÜNCELLEME ---
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var extension = Path.GetExtension(imageFile.FileName);
                        var newImageName = Guid.NewGuid() + extension;
                        var location = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/", newImageName);

                        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/")))
                            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/"));

                        using (var stream = new FileStream(location, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        trainerToUpdate.PhotoUrl = "/images/" + newImageName;
                    }
                    // ------------------------

                    // Hizmetleri Güncelle
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