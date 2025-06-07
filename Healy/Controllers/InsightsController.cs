using Healy.Models;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InsightsController : Controller
{
    private readonly IUserService _userService;
    private readonly IAiAnalysisService _aiAnalysisService;

    public InsightsController(IUserService userService, IAiAnalysisService aiAnalysisService)
    {
        _userService = userService;
        _aiAnalysisService = aiAnalysisService;
    }

    // GET: /Profile/{id}
    public async Task<IActionResult> Index(string email)
    {
        var user = await _userService.GetUserByEmailAsync("ariel.crb01@gmail.com");
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Deserialize all insights from user.Insights
        var insightsList = new List<InsightsData>();
        if (user.Insights != null && user.Insights.Any())
        {
            foreach (var insightJson in user.Insights)
            {
                try
                {
                    var insight = JsonConvert.DeserializeObject<InsightsData>(insightJson);
                    if (insight != null)
                    {
                        insightsList.Add(insight);
                    }
                }
                catch (JsonException ex)
                {
                    // Log the error and continue with other insights
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize insight: {ex.Message}");
                }
            }
        }

        // Pass the list of insights to the view
        ViewBag.InsightsDataList = insightsList;
        return View("~/Views/Home/Insights.cshtml", user);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateInsights(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null || string.IsNullOrEmpty(user.WearableData))
            return NotFound("User or wearable data not found.");

        // 1. Download CSV
        var csvContent = await _userService.DownloadCsvAsync(user.WearableData);

        // 2. Analyze CSV content
        var insights = await _aiAnalysisService.CsvAnalyzer(csvContent);

        // 3. Update insights
        user.Insights.Add(JsonConvert.SerializeObject(insights));
        await _userService.UpdateUserAsync(user);

        // 4. Rebuild the full list of insights
        var insightsList = new List<InsightsData>();
        if (user.Insights != null && user.Insights.Any())
        {
            foreach (var insightJson in user.Insights)
            {
                try
                {
                    var insight = JsonConvert.DeserializeObject<InsightsData>(insightJson);
                    if (insight != null)
                    {
                        insightsList.Add(insight);
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize insight: {ex.Message}");
                }
            }
        }

        // 5. Pass the updated list of insights to the view
        ViewBag.InsightsDataList = insightsList;
        return View("~/Views/Home/Insights.cshtml", user);
    }
}