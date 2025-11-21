using Microsoft.AspNetCore.Identity;
using FitnessApp.Web.Models;

namespace FitnessApp.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Kullanıcı ve Rol yöneticilerini servisten alıyoruz
            var userManager = service.GetService<UserManager<ApplicationUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

            // 1. Rolleri Oluştur (Admin ve Member)
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("Member"));

            // 2. Admin Kullanıcısını Oluştur
            // Ödevde istenen bilgiler: ogrencinumarasi@sakarya.edu.tr / sau
            var adminEmail = "g221210004@ogr.sakarya.edu.tr"; 
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                };

                // Kullanıcıyı oluşturuyoruz (Şifre: sau)
                // Identity şifre kuralları gereği genelde karmaşık şifre ister ama
                // burada basit şifre ayarını Program.cs'de yapmadıysak 'sau' hata verebilir.
                // Hata almamak için şifreyi 'Sau123!' yapalım, sonra giriş yapınca değiştiririz.
                // Veya Program.cs'de şifre kuralını gevşeteceğiz (Aşağıda yapacağız).
                
                var result = await userManager.CreateAsync(newAdmin, "sau"); 

                if (result.Succeeded)
                {
                    // Admin rolünü atıyoruz
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}