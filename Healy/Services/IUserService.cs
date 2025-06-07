using Healy.Models;
using Microsoft.Azure.Cosmos;

public interface IUserService
{
    Task<Healy.Models.User> GetUserByEmailAsync(string id);
    Task<string> DownloadCsvAsync(string csvUrl);
    Task UpdateUserAsync(Healy.Models.User user);
}
