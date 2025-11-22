using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Google.GenAI;
using System.Text.Json;

namespace FitnessApp.Web.Controllers
{
    [Authorize]
    public class AiController : Controller
    {
        private readonly string _apiKey;

        public AiController(IConfiguration config)
        {
            _apiKey = config["GoogleGeminiApiKey"];
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePlan(double height, double weight, string goal)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return View("Result", new AiAdvice
                {
                    analiz = "API anahtarı bulunamadı.",
                    diyet = "Lütfen appsettings.json içine API anahtarını ekleyin."
                });
            }

            string userPrompt =
                $"Ben {height} cm boyunda, {weight} kg ağırlığındayım. " +
                $"Hedefim: {GetGoalText(goal)}. " +
                $"BMI değerimi hesapla. " +
                $"Kısa bir egzersiz önerisi ver. " +
                $"1 günlük diyet menüsü yaz (Sabah, Öğle, Akşam). " +
                $"Cevabı SADECE şu formatta ver:\n\n" +
                "{\n" +
                "  \"analiz\": \"metin\",\n" +
                "  \"diyet\": \"metin\"\n" +
                "}";

            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_API_KEY", _apiKey);

                var client = new Client();
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.0-flash",
                    contents: userPrompt
                );

                string aiText = response?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";

                aiText = aiText.Replace("```json", "").Replace("```", "").Trim();

                AiAdvice parsed = null;

                try
                {
                    parsed = JsonSerializer.Deserialize<AiAdvice>(aiText);
                }
                catch
                {
                    // JSON bozuk dönerse yine de gösterelim
                    parsed = new AiAdvice
                    {
                        analiz = "JSON okunamadı. Ham çıktı gösteriliyor:\n\n" + aiText,
                        diyet = ""
                    };
                }

                return View("Result", parsed);
            }
            catch (Exception ex)
            {
                return View("Result", new AiAdvice
                {
                    analiz = "Bağlantı hatası oluştu.",
                    diyet = ex.Message
                });
            }
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
