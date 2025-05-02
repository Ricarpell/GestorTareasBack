using MongoDB.Driver;
using TaskManagerApi.Models;

namespace TaskManagerApi.Data
{
    public class TaskContext
{
    public IMongoCollection<Task> Tasks { get; }

    public TaskContext(IMongoClient client, IConfiguration configuration)
    {
        var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "TaskManager";
        var database = client.GetDatabase(databaseName);
        Tasks = database.GetCollection<Task>("Tasks");
    }
}
}
