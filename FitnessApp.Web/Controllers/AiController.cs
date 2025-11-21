using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FitnessApp.Web.Controllers
{
    [Authorize] 
    public class AiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePlan(double height, double weight, string goal)
        {
            // Yapay zeka düşünüyormuş gibi 1.5 saniye bekle (Gerçekçilik katar)
            await Task.Delay(1500);

            // 1. Vücut Kitle İndeksi Hesapla
            double heightM = height / 100;
            double bmi = weight / (heightM * heightM);
            
            string analiz = "";
            string diyet = "";

            // 2. Kural Tabanlı Yapay Zeka Mantığı
            // (Kullanıcının verilerine göre dinamik cevap üretir)
            if (goal == "lose_weight")
            {
                analiz = $"Analiz tamamlandı. BMI değeriniz: {bmi:F1}. Kilo verme hedefiniz doğrultusunda, yağ yakımını maksimize etmek için 'Yüksek Yoğunluklu Aralıklı Antrenman' (HIIT) programını sizin için seçtim. Metabolizmanızın hızlanması gerekiyor.";
                diyet = "Sabah: Yulaf ezmesi, tarçın ve 2 ceviz. \nAra Öğün: Yeşil elma. \nÖğle: Izgara tavuk göğsü ve bol yeşillik. \nAkşam: Zeytinyağlı kabak yemeği ve yoğurt.";
            }
            else if (goal == "muscle")
            {
                analiz = $"Analiz tamamlandı. BMI değeriniz: {bmi:F1}. Kas kütlesini artırmak (Hipertrofi) için progressive overload prensibine dayalı bir ağırlık antrenmanı planladım. Protein sentezini artırmamız kritik.";
                diyet = "Sabah: 3 yumurta (haşlanmış) ve lor peyniri. \nÖğle: 200gr Bulgur pilavı ve 150gr kırmızı et. \nAra Öğün: Muz ve fıstık ezmesi. \nAkşam: Fırında somon ve haşlanmış patates.";
            }
            else
            {
                analiz = $"Analiz tamamlandı. BMI değeriniz: {bmi:F1}. Mevcut formunuzu korumak ve postürünüzü iyileştirmek için Fonksiyonel Kuvvet antrenmanları uygundur. Denge ve esneklik çalışmalarına ağırlık verdim.";
                diyet = "Sabah: Tam tahıllı ekmek üzerine avokado ve beyaz peynir. \nÖğle: Mercimek çorbası ve mevsim salatası. \nAkşam: Izgara köfte, közlenmiş biber ve ayran.";
            }

            // 3. Sonuçları Ekrana Gönder
            ViewBag.Result = analiz;
            ViewBag.Diet = diyet;
            
            return View("Result");
        }
    }
}