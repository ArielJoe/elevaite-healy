using Healy.Models;
using Healy.Models.DTOs;

namespace Healy.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(RegisterDto registerDto);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        bool VerifyPassword(string password, string hash);
        string HashPassword(string password);
    }
}