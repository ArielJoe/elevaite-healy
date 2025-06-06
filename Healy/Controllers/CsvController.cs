using CsvHelper;
using CsvHelper.Configuration;
using Healy.Models;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Healy.Controllers
{
    public class CsvController : Controller
    {
        private readonly BlobService _blobService;

        public CsvController(BlobService blobService)
        {
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            // Replace this with your actual CSV file name in blob storage
            string blobFileName = "20250529_6804018672_MiFitness_hlth_center_fitness_data.csv";

            // Download the CSV file as a stream
            var stream = await _blobService.GetBlobStreamAsync(blobFileName);

            // Parse the CSV using CsvHelper
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            var records = csv.GetRecords<CsvRecordViewModel>().ToList();

            // Pass the records to the view
            return View(records);
        }
    }
}
