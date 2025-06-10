using Healy.Models;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class InsightsController : Controller
{
    private readonly IUserService _userService;
    private readonly IAiAnalysisService<InsightsData> _aiAnalysisService;
    private const int PageSize = 5; // Number of insights per page

    public InsightsController(IUserService userService, IAiAnalysisService<InsightsData> aiAnalysisService)
    {
        _userService = userService;
        _aiAnalysisService = aiAnalysisService;
    }

    // GET: /Insights
    public async Task<IActionResult> Index(string email, int page = 1)
    {
        var user = await _userService.GetUserByEmailAsync(HttpContext.Session.GetString("Email")!);
        if (user == null)
        {
            return RedirectToAction("Index", "Login");
            //return NotFound("User not found.");
        }

        // Deserialize all insights from user.Insights
        var allInsightsList = new List<InsightsData>();
        if (user.Insights != null && user.Insights.Any())
        {
            foreach (var insightJson in user.Insights)
            {
                try
                {
                    var insight = JsonConvert.DeserializeObject<InsightsData>(insightJson);
                    if (insight != null)
                    {
                        allInsightsList.Add(insight);
                    }
                }
                catch (JsonException ex)
                {
                    // Log the error and continue with other insights
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize insight: {ex.Message}");
                }
            }
        }

        // Sort insights by date/time (assuming you have a timestamp field)
        // If you don't have a timestamp, you might want to add one or sort by creation order
        allInsightsList = allInsightsList.OrderByDescending(x => x.CreatedAt ?? DateTime.Now).ToList();

        // Calculate pagination
        var totalInsights = allInsightsList.Count;
        var totalPages = (int)Math.Ceiling((double)totalInsights / PageSize);

        // Ensure page is within valid range
        page = Math.Max(1, Math.Min(page, totalPages));

        // Get insights for current page
        var pagedInsights = allInsightsList
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Create pagination info
        var paginationInfo = new PaginationInfo
        {
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalInsights,
            PageSize = PageSize,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };

        // Pass data to view
        ViewBag.InsightsDataList = pagedInsights;
        ViewBag.PaginationInfo = paginationInfo;
        ViewBag.Email = email;

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

        // 3. Add timestamp to insights
        insights.CreatedAt = DateTime.Now;

        // 4. Update insights
        user.Insights.Add(JsonConvert.SerializeObject(insights));
        await _userService.UpdateUserAsync(user);

        // 5. Redirect to first page to show new insights
        return RedirectToAction("Index", new { email = email, page = 1 });
    }
}