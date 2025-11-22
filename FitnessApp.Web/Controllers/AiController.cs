using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Google.GenAI;

namespace FitnessApp.Web.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly string _apiKey;

        public AiController(IConfiguration config)
        {
            // appsettings.json → "GoogleGeminiApiKey"
            _apiKey = config["GoogleGeminiApiKey"];
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePlan(double height, double weight, string goal)
        {
            // Eğer API key yoksa hata versin
            if (string.IsNullOrEmpty(_apiKey))
            {
                ViewBag.Result = "API anahtarı bulunamadı.";
                ViewBag.Diet = "Lütfen appsettings.json içine API anahtarını ekleyin.";
                return View("Result");
            }

            // 1. Prompt
            string userPrompt =
                $"Ben {height} cm boyunda, {weight} kg ağırlığında biriyim. " +
                $"Hedefim: {GetGoalText(goal)}. " +
                $"Bana BMI değerimi hesapla. " +
                $"Sonra kısa bir egzersiz önerisi ver. " +
                $"Ardından 1 günlük diyet menüsü yaz (Sabah, Öğle, Akşam). " +
                $"Cevabı sadece JSON olarak ver: " +
                $"{{ \"analiz\": \"...\", \"diyet\": \"...\" }}";

            try
            {
                // 2. API key'i environment'a set et
                Environment.SetEnvironmentVariable("GOOGLE_API_KEY", _apiKey);

                // 3. Client oluştur
                var client = new Client();

                // 4. İstek gönder
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.0-flash",
                    contents: userPrompt
                );

                // 5. Cevabı al
                string aiText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    ViewBag.Result = "Yapay zekadan boş cevap geldi.";
                    return View("Result");
                }

                // 6. Gereksiz markdown temizleme
                aiText = aiText.Replace("```json", "")
                               .Replace("```", "")
                               .Trim();

                // 7. JSON parse et
                try
                {
                    var parsed = JsonSerializer.Deserialize<AiAdvice>(aiText);

                    ViewBag.Result = parsed?.analiz ?? "Analiz bulunamadı.";
                    ViewBag.Diet = parsed?.diyet ?? "Diyet bulunamadı.";
                }
                catch
                {
                    // JSON bozuksa düz metin göster
                    ViewBag.Result = aiText;
                    ViewBag.Diet = "JSON formatı bozuk olduğu için düz metin gösterildi.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Result = "Bağlantı hatası oluştu.";
                ViewBag.Diet = ex.Message;
            }

            return View("Result");
        }

        private string GetGoalText(string goal) => goal switch
        {
            "lose_weight" => "Kilo vermek",
            "muscle" => "Kas yapmak",
            _ => "Formumu korumak"
        };

        public class AiAdvice
        {
            public string analiz { get; set; }
            public string diyet { get; set; }
        }
    }
}
