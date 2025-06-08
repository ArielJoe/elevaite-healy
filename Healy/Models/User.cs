using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Healy.Models
{
    public class User
    {
        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("username")]
        [JsonPropertyName("username")]
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("email")]
        [JsonPropertyName("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("password_hash")]
        [JsonPropertyName("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonProperty("birthdate")]
        [JsonPropertyName("birthdate")]
        [DataType(DataType.Date)]
        public DateTime Birthdate { get; set; }

        [JsonProperty("weight")]
        [JsonPropertyName("weight")]
        [Range(1, 1000)]
        public int Weight { get; set; }

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        [Range(1, 300)]
        public int Height { get; set; }

        [JsonProperty("wearable_data")]
        [JsonPropertyName("wearable_data")]
        public string WearableData { get; set; } = string.Empty;

        [JsonProperty("insights")]
        [JsonPropertyName("insights")]
        public List<string> Insights { get; set; } = new List<string>();

        [JsonProperty("activities")]
        [JsonPropertyName("activities")]
        public List<string> Activities { get; set; } = new List<string>();
    }
}