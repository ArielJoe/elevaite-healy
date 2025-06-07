using Healy.Models;
using Healy.Models.DTOs;
using Microsoft.Azure.Cosmos;
using BCrypt.Net;

namespace Healy.Services
{
    public class AuthService : IAuthService
    {
        private readonly Container _container;
        private readonly ILogger<AuthService> _logger;

        public AuthService(CosmosClient cosmosClient, ILogger<AuthService> logger)
        {
            _container = cosmosClient.GetContainer("Healy", "User");
            _logger = logger;
        }

        public async Task<Models.User> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                var user = new Models.User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = HashPassword(registerDto.Password),
                    Birthdate = registerDto.Birthdate,
                    Weight = registerDto.Weight,
                    Height = registerDto.Height
                };

                var response = await _container.CreateItemAsync(user, new PartitionKey(user.Id));
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", registerDto.Email);
                throw;
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.username = @username")
                    .WithParameter("@username", username);

                var iterator = _container.GetItemQueryIterator<Models.User>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (response.Any())
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private async Task<Models.User> GetUserByEmailAsync(string email)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                .WithParameter("@email", email);

            var iterator = _container.GetItemQueryIterator<Models.User>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault()!;
            }

            return null!;
        }
    }
}