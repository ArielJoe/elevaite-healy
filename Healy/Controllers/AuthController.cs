using Healy.Models.DTOs;
using Healy.Services;
using Microsoft.AspNetCore.Mvc;

namespace Healy.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return View(loginDto);

            var user = await _authService.EmailExistsAsync(loginDto.Email)
                ? await _authService.GetUserByEmailAsync(loginDto.Email)
                : null;

            if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginDto);
            }

            //HttpContext.Session.SetString("UserId", "0001");
            //HttpContext.Session.SetString("Username", "ArielJoe");
            //HttpContext.Session.SetString("Email", "durensaospadang@gmail.com");
            //HttpContext.Session.SetInt32("Weight", 80);
            //HttpContext.Session.SetInt32("Height", 179);
            //HttpContext.Session.SetString("Birthdate", "2005-04-28");
            //HttpContext.Session.SetString("Gender", "Male");

            //HttpContext.Session.SetString("UserId", user.Id);
            //HttpContext.Session.SetString("Username", user.Username);
            //HttpContext.Session.SetString("Email", user.Email);
            //HttpContext.Session.SetInt32("Weight", user.Weight);
            //HttpContext.Session.SetInt32("Height", user.Height);
            //HttpContext.Session.SetString("Birthdate", user.Birthdate.ToString("yyyy-MM-dd"));
            //HttpContext.Session.SetString("Gender", user.Gender!);

            return RedirectToAction("Index", "Home");
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            _logger.LogInformation("Register request received");

            if (!ModelState.IsValid)
            {
                return View(registerDto);
            }

            if (await _authService.EmailExistsAsync(registerDto.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(registerDto);
            }

            if (await _authService.UsernameExistsAsync(registerDto.Username))
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                return View(registerDto);
            }

            try
            {
                var user = await _authService.RegisterAsync(registerDto);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(registerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                ModelState.AddModelError(string.Empty, "An error occurred while registering your account.");
                return View(registerDto);
            }

            return RedirectToAction("Login");
        }

        // Helper method to retrieve user (replicates what AuthService does internally)
        //private async Task<Models.User> GetUserByEmail(string email)
        //{
        //    var query = new Microsoft.Azure.Cosmos.QueryDefinition("SELECT * FROM c WHERE c.email = @email")
        //        .WithParameter("@email", email);

        //    var container = ((AuthService)_authService).GetType()
        //        .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
        //        .GetValue(_authService) as Microsoft.Azure.Cosmos.Container;

        //    if (container == null)
        //        return null!;

        //    var iterator = container.GetItemQueryIterator<Models.User>(query);
        //    while (iterator.HasMoreResults)
        //    {
        //        var response = await iterator.ReadNextAsync();
        //        return response.FirstOrDefault()!;
        //    }

        //    return null!;
        //}
    }
}