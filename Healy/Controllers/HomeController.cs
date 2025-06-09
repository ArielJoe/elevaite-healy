using CsvHelper;
using CsvHelper.Configuration;
using Healy.Models;
using Healy.Models.DTOs;
using Healy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Healy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BlobService _blobService;
        private readonly IAuthService _authService;

        public HomeController(ILogger<HomeController> logger, BlobService blobService, IAuthService authService)
        {
            _logger = logger;
            _blobService = blobService;
            _authService = authService;
        }

        public async Task<IActionResult> Index(string period = "daily")
        {
            _logger.LogInformation("Index action started at {Timestamp}", DateTime.UtcNow);

            string blobFileName = "20250529_6804018672_MiFitness_hlth_center_fitness_data.csv";

            try
            {
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

                var combinedViewModel = new CombinedHealthMetricViewModel();

                if (records == null || !records.Any())
                {
                    _logger.LogWarning("No records found in CSV.");
                    return View(combinedViewModel);
                }

                var recordsWithDates = records.Select(r => new
                {
                    r.Uid,
                    r.Sid,
                    r.Key,
                    Time = DateTimeOffset.FromUnixTimeSeconds(r.Time).UtcDateTime,
                    r.Value,
                    r.UpdateTime
                }).ToList();

                DateTime latestTime = recordsWithDates.Max(r => r.Time);
                DateTime latestWib = latestTime.AddHours(7); // Convert UTC to WIB

                // Define time periods
                DateTime oneDayAgo = latestWib.AddDays(-1);
                DateTime oneWeekAgo = latestWib.AddDays(-7);
                DateTime oneMonthAgo = latestWib.AddDays(-30);

                // Aggregate data
                var dailyData = recordsWithDates
                    .Where(r => r.Time.Date == latestWib.Date)
                    .GroupBy(r => r.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value);

                var weeklyData = recordsWithDates
                    .Where(r => r.Time >= oneWeekAgo && r.Time <= latestWib)
                    .GroupBy(r => r.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value);

                var monthlyData = recordsWithDates
                    .Where(r => r.Time >= oneMonthAgo && r.Time <= latestWib)
                    .GroupBy(r => r.Key)
                    .ToDictionary(g => g.Key, g => g.Last().Value);

                // Update view model with aggregated data
                combinedViewModel.Daily = CreateHealthMetricViewModel(dailyData);
                combinedViewModel.Weekly = CreateHealthMetricViewModel(weeklyData);
                combinedViewModel.Monthly = CreateHealthMetricViewModel(monthlyData);

                return View(combinedViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Index action");
                return View(new CombinedHealthMetricViewModel());
            }
        }

        private HealthMetricViewModel CreateHealthMetricViewModel(Dictionary<string, string> data)
        {
            return new HealthMetricViewModel
            {
                HeartRate = GetParsedValue(data, "heart_rate"),
                Sleep = GetParsedValue(data, "sleep"),
                BloodOxygen = GetParsedValue(data, "spo2"),
                Steps = GetParsedValue(data, "steps"),
                Calories = GetParsedValue(data, "calories"),
                Water = GetParsedValue(data, "water"),
                Stress = GetParsedValue(data, "stress")
            };
        }

        private string GetParsedValue(Dictionary<string, string> data, string key)
        {
            if (data.TryGetValue(key, out var value))
            {
                try
                {
                    using var document = JsonDocument.Parse(value);
                    var root = document.RootElement;

                    return key switch
                    {
                        "heart_rate" => root.GetProperty("bpm").GetInt32().ToString(),
                        "sleep" => (root.GetProperty("duration").GetInt32() / 60.0).ToString("F1"),
                        "spo2" => root.GetProperty("spo2").GetInt32().ToString(),
                        "steps" => root.GetProperty("steps").GetInt32().ToString(),
                        "calories" => root.GetProperty("calories").GetDouble().ToString("F1"),
                        "water" => root.GetProperty("water").GetDouble().ToString("F1"),
                        "stress" => root.GetProperty("stress").GetInt32().ToString(),
                        _ => ""
                    };
                }
                catch
                {
                    return "";
                }
            }
            return "";
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for registration: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(registerDto);
            }

            try
            {
                var user = await _authService.RegisterAsync(registerDto);
                _logger.LogInformation("User registered successfully: {Email}", user.Email);

                // Optional: Auto-login after registration using session
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);

                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(registerDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validation error during registration: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(registerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email: {Email}", registerDto.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration.");
                return View(registerDto);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(loginDto);
            }

            var user = await _authService.EmailExistsAsync(loginDto.Email)
                ? await _authService.GetUserByEmailAsync(loginDto.Email)
                : null;

            if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid login attempt for email: {Email}", loginDto.Email);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginDto);
            }

            // Store user data in session
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            _logger.LogInformation("User logged out");
            return RedirectToAction("Login", "Home");
        }

        public IActionResult Profile()
        {
            // Optional: Check if user is logged in
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                _logger.LogWarning("Unauthorized access to Profile page");
                return RedirectToAction("Login", "Home");
            }
            return View();
        }

        public IActionResult Insights()
        {
            // Optional: Check if user is logged in
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                _logger.LogWarning("Unauthorized access to Insights page");
                return RedirectToAction("Login", "Home");
            }

            ViewBag.InsightsData = new
            {
                SleepQuality = new { Title = "Sleep Quality Declining", Time = "06:07 AM", Description = "Your deep sleep has decreased by 20% over the past week, which may affect your recovery and cognitive function.", Recommendation = "Try to maintain a consistent sleep schedule and avoid screens 1 hour before bedtime." },
                ActivityPattern = new { Title = "Activity Pattern Change", Time = "06:07 AM", Description = "You've been less active on weekdays compared to your previous month's average.", Recommendation = "Consider scheduling short walks during your work breaks to increase daily activity." },
                HydrationImprovement = new { Title = "Hydration Improvement", Time = "06:07 AM", Description = "Your hydration consistency has improved by 25% this week.", Recommendation = "Keep using timed water reminders to maintain this positive habit." },
                StressLevels = new { Title = "Elevated Stress Levels", Time = "10:07 AM", Description = "Your stress levels have been consistently higher than normal for the past 3 days.", Recommendation = "Try the guided breathing exercises in the app for 5 minutes, 3 times daily." },
                WeeklySummary = new { Title = "Your Weekly Health Summary", Description = "Based on your data from this week, your overall health indicators are showing positive trends. Your sleep quality has improved by 15%, and your heart rate variability indicates good recovery. Keep maintaining your evening meditation routine and consistent sleep schedule.", Changes = new[] { new { Category = "Sleep Quality", Change = "+15%" }, new { Category = "Stress Level", Change = "-8%" }, new { Category = "Activity", Change = "+12%" } } }
            };
            return View();
        }

        public IActionResult Activities()
        {
            // Optional: Check if user is logged in
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                _logger.LogWarning("Unauthorized access to Activities page");
                return RedirectToAction("Login", "Home");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}