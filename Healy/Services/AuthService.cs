using BCrypt.Net;
using Healy.Models;
using Healy.Models.DTOs;
using Microsoft.Azure.Cosmos;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

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
                _logger.LogInformation("Starting registration for email: {Email}", registerDto.Email);

                // Validate input
                if (registerDto == null)
                {
                    throw new ArgumentNullException(nameof(registerDto), "Registration data cannot be null");
                }

                // Validate required fields
                var validationContext = new ValidationContext(registerDto);
                Validator.ValidateObject(registerDto, validationContext, validateAllProperties: true);

                // Check if email already exists
                if (await EmailExistsAsync(registerDto.Email))
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", registerDto.Email);
                    throw new InvalidOperationException("User with this email already exists");
                }

                // Check if username already exists
                if (await UsernameExistsAsync(registerDto.Username))
                {
                    _logger.LogWarning("Registration failed: Username {Username} already exists", registerDto.Username);
                    throw new InvalidOperationException("User with this username already exists");
                }

                // Create new user
                var user = new Models.User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = registerDto.Username,
                    Email = registerDto.Email?.ToLowerInvariant() ?? throw new ArgumentException("Email is required"),
                    PasswordHash = HashPassword(registerDto.Password),
                    Birthdate = registerDto.Birthdate,
                    Weight = registerDto.Weight,
                    Height = registerDto.Height,
                    WearableData = string.Empty,
                    Insights = new List<string>(),
                    Activities = new List<string>()
                };

                // Validate user model
                validationContext = new ValidationContext(user);
                Validator.ValidateObject(user, validationContext, validateAllProperties: true);

                // Save to database
                _logger.LogInformation("Saving user to Cosmos DB with ID: {Id}", user.Id);
                var response = await _container.CreateItemAsync(
                    user,
                    new PartitionKey(user.Id)
                ).ConfigureAwait(false);

                _logger.LogInformation("Successfully registered user with email: {Email}", user.Email);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning("User registration conflict for email: {Email}", registerDto.Email);
                throw new InvalidOperationException("User with this email already exists", ex);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Database error during registration for email: {Email}", registerDto.Email);
                throw new Exception("Failed to register user due to database error", ex);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation failed for user registration: {Message}", ex.Message);
                throw new ArgumentException($"Validation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email: {Email}", registerDto.Email);
                throw new Exception("An unexpected error occurred during registration", ex);
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username existence: {Username}", username);
                throw;
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

        public async Task<Models.User> GetUserByEmailAsync(string email)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                    .WithParameter("@email", email.ToLowerInvariant());
                var iterator = _container.GetItemQueryIterator<Models.User>(query);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }
    }
}