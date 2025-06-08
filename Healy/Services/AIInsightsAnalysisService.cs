using Azure.AI.OpenAI;
using Healy.Models;
using Healy.Services;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AIInsightsAnalysisService : IAiAnalysisService<InsightsData>
{
    private readonly ChatClient _chatClient;

    public AIInsightsAnalysisService(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<InsightsData> CsvAnalyzer(string csvContent)
    {
        // Calculate half length to take half the string
        int halfLength = csvContent.Length / 2;
        // Get the substring representing the first half of the CSV content
        string csvHalfContent = csvContent.Substring(0, halfLength);

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("IMPORTANT: Your response must be valid JSON only. Do not include any other text or explanations.");
        promptBuilder.AppendLine("You are a professional health assistant.");
        promptBuilder.AppendLine("Analyze the following wearable health CSV data and generate insights for only ONE of these categories:");
        promptBuilder.AppendLine("- HeartRate");
        promptBuilder.AppendLine("- Sleep");
        promptBuilder.AppendLine("- BloodO2");
        promptBuilder.AppendLine("- Steps");
        promptBuilder.AppendLine("- Calories");
        promptBuilder.AppendLine("- Stress");
        promptBuilder.AppendLine("Return ONLY a JSON object with this exact schema:");
        promptBuilder.AppendLine(@"{
  ""Category"": """",
  ""Title"": """",
  ""Description"": """",
  ""Recommendation"": """",
  ""Time"": """"
}");
        promptBuilder.AppendLine("The Category field must be exactly one of: HeartRate, Sleep, BloodO2, Steps, Calories, or Stress");
        promptBuilder.AppendLine("Select the category you find most significant based on the data. Provide a clear, brief insight and recommendation relevant to that category only.");
        promptBuilder.AppendLine("The time is in UNIX format, convert it to human format.");
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
            var tempResult = JsonSerializer.Deserialize<TempInsightResult>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tempResult == null)
            {
                return GetFallbackInsights();
            }

            // Create InsightsData and populate the appropriate category
            var insights = new InsightsData();
            var insightCategory = new InsightCategory
            {
                Title = tempResult.Title,
                Description = tempResult.Description,
                Recommendation = tempResult.Recommendation,
                Time = tempResult.Time ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Assign to the correct category based on the response
            switch (tempResult.Category?.ToLower())
            {
                case "heartrate":
                    insights.HeartRate = insightCategory;
                    break;
                case "sleep":
                    insights.Sleep = insightCategory;
                    break;
                case "bloodo2":
                    insights.BloodO2 = insightCategory;
                    break;
                case "steps":
                    insights.Steps = insightCategory;
                    break;
                case "calories":
                    insights.Calories = insightCategory;
                    break;
                case "stress":
                    insights.Stress = insightCategory;
                    break;
                default:
                    // Default to HeartRate if category is not recognized
                    insights.HeartRate = insightCategory;
                    break;
            }

            return insights;
        }
        catch (JsonException ex)
        {
            // Log the error and return fallback
            System.Diagnostics.Debug.WriteLine($"JSON parsing failed: {ex.Message}");
            return GetFallbackInsights();
        }
        catch (Exception ex)
        {
            // Log the error and return fallback
            System.Diagnostics.Debug.WriteLine($"AI analysis failed: {ex.Message}");
            return GetFallbackInsights();
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

    private InsightsData GetFallbackInsights()
    {
        return new InsightsData
        {
            HeartRate = new InsightCategory
            {
                Title = "Health Data Analysis",
                Description = "Your health data has been recorded. Continue monitoring your daily activities for better insights.",
                Recommendation = "Maintain consistent daily routines and regular health monitoring.",
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
    }

    // Temporary class to deserialize the AI response
    private class TempInsightResult
    {
        public string? Category { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Recommendation { get; set; }
        public string? Time { get; set; }
    }
}