using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace FitnessApp.Web.Services
{
    // Bu sınıf sahte bir email göndericidir. Hata almamak için "göndermiş gibi" yapar.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Gerçekten mail atma, sadece görevi tamamlandı say.
            return Task.CompletedTask;
        }
    }
}