using System.Text.Json;

namespace Healy.Models
{
    public class CsvRecordViewModel
    {
        public required string Uid { get; set; }
        public required string Sid { get; set; }
        public required string Key { get; set; }
        public long Time { get; set; }
        public required string Value { get; set; }
        public long UpdateTime { get; set; }

        public string GetParsedValue()
        {
            try
            {
                using var document = JsonDocument.Parse(Value);
                var root = document.RootElement;

                return Key switch
                {
                    "heart_rate" => root.GetProperty("bpm").GetInt32().ToString(),
                    "sleep" => (root.GetProperty("duration").GetInt32() / 60.0).ToString("F1"), // Convert minutes to hours
                    "spo2" => root.GetProperty("spo2").GetInt32().ToString(),
                    "steps" => root.GetProperty("steps").GetInt32().ToString(),
                    "calories" => root.GetProperty("calories").GetDouble().ToString("F1"),
                    "water" => root.GetProperty("water").GetDouble().ToString("F1"),
                    "stress" => root.GetProperty("stress").GetInt32().ToString(),
                    _ => "N/A"
                };
            }
            catch
            {
                return "N/A";
            }
        }
    }
}