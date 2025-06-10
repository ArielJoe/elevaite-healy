using Healy.Models;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ActivitiesController : Controller
{
    private readonly IUserService _userService;
    private readonly IAiAnalysisService<ActivitiesData> _aiAnalysisService;
    private const int PageSize = 5;

    public ActivitiesController(IUserService userService, IAiAnalysisService<ActivitiesData> aiAnalysisService)
    {
        _userService = userService;
        _aiAnalysisService = aiAnalysisService;
    }

    public async Task<IActionResult> Index(string email, int page = 1)
    {
        var user = await _userService.GetUserByEmailAsync(HttpContext.Session.GetString("Email")!);
        if (user == null)
        {
            return RedirectToAction("Index", "Login");

            //System.Diagnostics.Debug.WriteLine("User not found for email: " + email);
            //return NotFound("User not found.");
        }

        // Log the raw activities data
        System.Diagnostics.Debug.WriteLine($"Raw user.Activities: {string.Join(", ", user.Activities ?? new List<string>())}");

        // Deserialize all activities from user.Activities into List<ActivitiesData>
        var allActivitiesList = new List<ActivitiesData>();
        if (user.Activities != null && user.Activities.Any())
        {
            foreach (var activityJson in user.Activities)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Deserializing activity: {activityJson}");
                    var activity = JsonConvert.DeserializeObject<ActivitiesData>(
                        activityJson,
                        new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    if (activity != null)
                    {
                        allActivitiesList.Add(activity);
                        System.Diagnostics.Debug.WriteLine($"Successfully deserialized activity: {JsonConvert.SerializeObject(activity)}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Deserialized activity is null");
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize activity: {ex.Message}");
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No activities found for user.");
        }

        // Log the deserialized activities count
        System.Diagnostics.Debug.WriteLine($"Total activities deserialized: {allActivitiesList.Count}");

        // Sort activities by CreatedAt (descending)
        allActivitiesList = allActivitiesList
            .OrderByDescending(x => x.CreatedAt ?? DateTime.Now)
            .ToList();

        // Calculate pagination
        var totalActivities = allActivitiesList.Count;
        var totalPages = (int)Math.Ceiling((double)totalActivities / PageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        // Get activities for current page
        var pagedActivities = allActivitiesList
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Log paged activities
        System.Diagnostics.Debug.WriteLine($"Paged activities count: {pagedActivities.Count}");

        // Create pagination info
        var paginationInfo = new PaginationInfo
        {
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalActivities,
            PageSize = PageSize,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        };

        // Pass data to view
        ViewBag.ActivitiesDataList = pagedActivities;
        ViewBag.PaginationInfo = paginationInfo;
        ViewBag.Email = email;
        return View("~/Views/Home/Activities.cshtml", user);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateActivities(string email)
    {
        var user = await _userService.GetUserByEmailAsync(HttpContext.Session.GetString("Email")!);
        if (user == null || string.IsNullOrEmpty(user.WearableData))
            return NotFound("User or wearable data not found.");

        var csvContent = await _userService.DownloadCsvAsync(user.WearableData);
        var activities = await _aiAnalysisService.CsvAnalyzer(csvContent);
        activities.CreatedAt = DateTime.Now;

        user.Activities.Add(JsonConvert.SerializeObject(activities));
        await _userService.UpdateUserAsync(user);

        return RedirectToAction("Index", new { email = email, page = 1 });
    }
}