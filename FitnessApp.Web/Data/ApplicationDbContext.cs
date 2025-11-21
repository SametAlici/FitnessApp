using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FitnessApp.Web.Models; 

namespace FitnessApp.Web.Data
{
    // Standart IdentityDbContext yerine, kendi User sınıfımızla (ApplicationUser) türetiyoruz
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanında oluşacak Tablolar
        public DbSet<Service> Services { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -- İlişki Ayarları --

            // Many-to-Many: Bir Antrenörün çok Hizmeti, bir Hizmetin çok Antrenörü olur.
            // Ara tablo ismini "TrainerServices" yapıyoruz.
            builder.Entity<Trainer>()
                .HasMany(t => t.Services)
                .WithMany(s => s.Trainers)
                .UsingEntity(j => j.ToTable("TrainerServices")); 

            // -- Silme Davranışları (Hata Önleme) --
            // SQL Server'da "Multiple Cascade Paths" hatasını önlemek için Restrict kullanıyoruz.
            // Yani: Randevusu olan bir antrenörü direkt silemezsin, önce randevuyu iptal etmelisin.

            builder.Entity<Appointment>()
                .HasOne(a => a.Trainer)
                .WithMany()
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

                builder.Entity<Service>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");
        }
    }
}