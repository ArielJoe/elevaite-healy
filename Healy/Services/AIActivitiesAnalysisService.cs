using Azure.AI.OpenAI;
using Healy.Models;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healy.Services
{
    public class AIActivitiesAnalysisService : IAiAnalysisService<ActivitiesData>
    {
        private readonly ChatClient _chatClient;

        public AIActivitiesAnalysisService(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public async Task<ActivitiesData> CsvAnalyzer(string csvContent)
        {
            // Calculate half length to take half the string
            int halfLength = csvContent.Length / 2;
            // Get the substring representing the first half of the CSV content
            string csvHalfContent = csvContent.Substring(0, halfLength);

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("IMPORTANT: Your response must be valid JSON only. Do not include any other text or explanations.");
            promptBuilder.AppendLine("You are a professional health assistant.");
            promptBuilder.AppendLine("Analyze the following wearable health CSV data and generate activity recommendations for only ONE of these categories:");
            promptBuilder.AppendLine("- Walking");
            promptBuilder.AppendLine("- Breathing");
            promptBuilder.AppendLine("- WeightLifting");
            promptBuilder.AppendLine("Return ONLY a JSON object with this exact schema:");
            promptBuilder.AppendLine(@"{
  ""Category"": """",
  ""Title"": """",
  ""Description"": """",
  ""Recommendation"": """",
  ""Time"": """"
}");
            promptBuilder.AppendLine("Select the category you find most significant based on the data. Provide a clear, brief activity recommendation relevant to that category only.");
            promptBuilder.AppendLine("The time is in UNIX format, convert it to human format.");
            promptBuilder.AppendLine("GIVE RECOMMENDATION THAT NOT BURDENSOME, JUST 2 TO 10 MINUTRES OF DAILY ACTIVITY.");
            promptBuilder.AppendLine("Accumulate the informations first before making decisiion.");
            promptBuilder.AppendLine("Here is the CSV data:");
            promptBuilder.AppendLine(csvHalfContent);

            var chatHistory = new List<ChatMessage>
            {
                new SystemChatMessage(promptBuilder.ToString())
            };

            try
            {
                var response = await _chatClient.CompleteChatAsync(chatHistory);
                var rawResponse = response.Value.Content[0].Text.Trim();

                // Try to find JSON within the response
                var jsonContent = ExtractJsonFromResponse(rawResponse);

                // First deserialize to a temporary object to get category and data
                var tempResult = JsonSerializer.Deserialize<TempActivityResult>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tempResult == null)
                {
                    return GetFallbackActivities();
                }

                // Create ActivitiesData and populate the appropriate category
                var activities = new ActivitiesData();
                var activityCategory = new ActivityCategory
                {
                    Title = tempResult.Title,
                    Description = tempResult.Description,
                    Recommendation = tempResult.Recommendation,
                    Time = tempResult.Time ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // Assign to the correct category based on the response
                switch (tempResult.Category?.ToLower())
                {
                    case "waking":
                        activities.Walking = activityCategory;
                        break;
                    case "breathing":
                        activities.Breathing = activityCategory;
                        break;
                    case "weightlifting":
                        activities.WeightLifting = activityCategory;
                        break;
                    default:
                        // Default to Walking if category is not recognized
                        activities.Walking = activityCategory;
                        break;
                }

                return activities;
            }
            catch (JsonException ex)
            {
                // Log the error and return fallback
                System.Diagnostics.Debug.WriteLine($"JSON parsing failed: {ex.Message}");
                return GetFallbackActivities();
            }
            catch (Exception ex)
            {
                // Log the error and return fallback
                System.Diagnostics.Debug.WriteLine($"AI analysis failed: {ex.Message}");
                return GetFallbackActivities();
            }
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Remove any markdown code blocks
            response = response.Replace("```json", "").Replace("```", "").Trim();

            // Find the first { and last } to extract JSON
            int startIndex = response.IndexOf('{');
            int lastIndex = response.LastIndexOf('}');

            if (startIndex >= 0 && lastIndex > startIndex)
            {
                return response.Substring(startIndex, lastIndex - startIndex + 1);
            }

            // If no braces found, assume the entire response is JSON
            return response;
        }

        private ActivitiesData GetFallbackActivities()
        {
            return new ActivitiesData
            {
                Walking = new ActivityCategory
                {
                    Title = "Activity Recommendation",
                    Description = "Your activity data has been recorded. Continue monitoring your daily activities for better recommendations.",
                    Recommendation = "Engage in light physical activity, such as a 15-minute walk, to maintain an active lifestyle.",
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        // Temporary class to deserialize the AI response
        private class TempActivityResult
        {
            public string? Category { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Recommendation { get; set; }
            public string? Time { get; set; }
        }
    }
}