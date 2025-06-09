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
                ? await GetUserByEmail(loginDto.Email)
                : null;

            if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginDto);
            }

            // TODO: Add session or authentication logic here
            // e.g., HttpContext.Session.SetString("UserId", user.Id);

            return RedirectToAction("Index", "Home"); // Redirect after successful login
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (await _authService.EmailExistsAsync(registerDto.Email))
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            _logger.LogInformation("Register");

            if (!ModelState.IsValid) {
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

            var user = await _authService.RegisterAsync(registerDto);

            // Optional: Automatically log in user after registration

            return RedirectToAction("Login");
        }

        // Helper method to retrieve user (replicates what AuthService does internally)
        private async Task<Models.User> GetUserByEmail(string email)
        {
            var query = new Microsoft.Azure.Cosmos.QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                .WithParameter("@email", email);

            var container = ((AuthService)_authService).GetType()
                .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(_authService) as Microsoft.Azure.Cosmos.Container;

            if (container == null)
                return null!;

            var iterator = container.GetItemQueryIterator<Models.User>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault()!;
            }

            return null!;
        }
    }
}