using CsvHelper;
using CsvHelper.Configuration;
using Healy.Models;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Healy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BlobService _blobService; // Inject BlobService

        public HomeController(ILogger<HomeController> logger, BlobService blobService)
        {
            _logger = logger;
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            string blobFileName = "20250529_6804018672_MiFitness_hlth_center_fitness_data.csv";
            var stream = await _blobService.GetBlobStreamAsync(blobFileName);

            List<CsvRecordViewModel> records;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            }))
            {
                records = csv.GetRecords<CsvRecordViewModel>().ToList();
            }

            var viewModel = new HealthMetricViewModel
            {
                HeartRate = records.LastOrDefault(r => r.Key == "heart_rate")?.GetParsedValue() ?? "N/A",
                Sleep = records.LastOrDefault(r => r.Key == "sleep")?.GetParsedValue() ?? "N/A",
                BloodOxygen = records.LastOrDefault(r => r.Key == "spo2")?.GetParsedValue() ?? "N/A",
                Steps = records.LastOrDefault(r => r.Key == "steps")?.GetParsedValue() ?? "N/A",
                Calories = records.LastOrDefault(r => r.Key == "calories")?.GetParsedValue() ?? "N/A",
                Water = records.LastOrDefault(r => r.Key == "water")?.GetParsedValue() ?? "N/A",
                Stress = records.LastOrDefault(r => r.Key == "stress")?.GetParsedValue() ?? "N/A"
            };

            return View(viewModel);
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Insights()
        {
            return View();
        }

        public IActionResult Activities()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}