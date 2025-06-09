using Microsoft.Azure.Cosmos;
using System.Reflection;
using Healy.Models;
using Healy.Services;

public class UserService : IUserService
{
    private readonly Container _container;

    public UserService(CosmosClient cosmosClient, IConfiguration config)
    {
        var dbName = config["CosmosDb:DatabaseName"];
        var containerName = config["CosmosDb:ContainerName"];
        _container = cosmosClient.GetContainer(dbName, containerName);
    }

    public async Task<Healy.Models.User> GetUserByEmailAsync(string email)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
            .WithParameter("@email", email);

        using var iterator = _container.GetItemQueryIterator<Healy.Models.User>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault()!; // Return the first matching user
        }

        return null!;
    }

    public async Task<string> DownloadCsvAsync(string csvUrl)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(csvUrl);
    }

    public async Task UpdateUserAsync(Healy.Models.User user)
    {
        await _container.UpsertItemAsync(user, new PartitionKey(user.Email));
    }

}
